using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.Windows.OwnerDraw;
using II.RPA.Plugin.Windows.OwnerDraw.PowerBuilder;
using II.RPA.Plugin.Windows.OwnerDraw.Vcl;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using Xunit;

namespace II.RPA.Plugin.Windows.OwnerDraw.Tests
{
    public sealed class OwnerDrawSelectorTests
    {
        [Fact]
        public void VirtualCellOverridesEveryPublicGuiNodeContractMember()
        {
            Type implementation = typeof(OwnerDrawVirtualElementNode);
            Type[] roots =
            {
                typeof(IGuiNode_6_6_0),
                typeof(IGuiWindowsNode),
            };
            Type[] contracts = roots.SelectMany(root =>
                    new[] { root }.Concat(root.GetInterfaces()))
                .Distinct()
                .ToArray();

            var inherited = new List<string>();
            foreach (Type contract in contracts)
            {
                InterfaceMapping map = implementation.GetInterfaceMap(contract);
                for (int index = 0; index < map.InterfaceMethods.Length; index++)
                {
                    if (map.TargetMethods[index].DeclaringType != implementation)
                    {
                        inherited.Add(contract.Name + "."
                            + map.InterfaceMethods[index].Name + " -> "
                            + map.TargetMethods[index].DeclaringType.FullName);
                    }
                }
            }

            Assert.True(inherited.Count == 0,
                "Virtual Cell leaked interface members to UIANode: "
                + string.Join(", ", inherited));
        }

        [Fact]
        public void ConflictingIdentityKindIsRejected()
        {
            List<OwnerDrawSelectorProperty> properties = new List<OwnerDrawSelectorProperty>(
                OwnerDrawSelectorCodec.Encode(PowerBuilderGridSelector()));
            int index = properties.FindIndex(property =>
                property.Name == OwnerDrawSelectorCodec.IdentityStrength);
            properties[index] = new OwnerDrawSelectorProperty(
                OwnerDrawSelectorCodec.IdentityStrength, "visual-index", true);

            Assert.False(OwnerDrawSelectorCodec.TryDecode(properties,
                out _, out string status));
            Assert.Contains("conflicts", status);
        }

        [Fact]
        public void InexactIdentityPropertyIsRejected()
        {
            List<OwnerDrawSelectorProperty> properties = new List<OwnerDrawSelectorProperty>(
                OwnerDrawSelectorCodec.Encode(PowerBuilderGridSelector()));
            int index = properties.FindIndex(property =>
                property.Name == OwnerDrawSelectorCodec.PbCol);
            properties[index] = new OwnerDrawSelectorProperty(
                OwnerDrawSelectorCodec.PbCol, "2", false);

            Assert.False(OwnerDrawSelectorCodec.TryDecode(properties,
                out _, out string status));
            Assert.Contains("must be present and exact", status);
        }

        [Fact]
        public void DuplicateReservedPropertyWithAccuracyConflictIsRejected()
        {
            List<OwnerDrawSelectorProperty> properties = new List<OwnerDrawSelectorProperty>(
                OwnerDrawSelectorCodec.Encode(PowerBuilderGridSelector()));
            properties.Add(new OwnerDrawSelectorProperty(
                OwnerDrawSelectorCodec.PbCol, "99", false));

            Assert.False(OwnerDrawSelectorCodec.TryDecode(properties,
                out _, out string status));
            Assert.Contains("duplicate reserved property", status);

            properties = new List<OwnerDrawSelectorProperty>(
                OwnerDrawSelectorCodec.Encode(PowerBuilderGridSelector()));
            properties.Add(new OwnerDrawSelectorProperty(
                OwnerDrawSelectorCodec.PbCol.ToUpperInvariant(), "17", true));
            Assert.False(OwnerDrawSelectorCodec.TryDecode(properties,
                out _, out status));
            Assert.Contains("duplicate reserved property", status);
        }

        [Fact]
        public void ViewportOnlyVclSelectorIsRejected()
        {
            var selector = new OwnerDrawSelector
            {
                SchemaVersion = OwnerDrawSelectorCodec.CurrentSchemaVersion,
                AdapterId = OwnerDrawSelectorCodec.VclAdapterId,
                HostClassName = "TFDBGrid",
                IdentityStrength = OwnerDrawIdentityStrength.LogicalCell,
                LogicalRow = int.MinValue,
                LogicalColumn = int.MinValue,
            };

            Assert.False(OwnerDrawSelectorCodec.TryValidate(selector,
                out string status));
            Assert.Contains("logical row/column", status);
        }

        [Fact]
        public void LegacyVclSelectorRejectsDisabledLogicalIdentity()
        {
            var properties = new List<OwnerDrawSelectorProperty>
            {
                new OwnerDrawSelectorProperty(
                    OwnerDrawSelectorCodec.VclGridCell, "true", true),
                new OwnerDrawSelectorProperty(
                    OwnerDrawSelectorCodec.VclGridClass, "TFDBGrid", true),
                new OwnerDrawSelectorProperty(
                    OwnerDrawSelectorCodec.VclGridRow, "3", false),
                new OwnerDrawSelectorProperty(
                    OwnerDrawSelectorCodec.VclGridColumn, "2", true),
            };

            Assert.False(OwnerDrawSelectorCodec.TryDecode(properties,
                out _, out string status));
            Assert.Contains("must be present and exact", status);
        }

        [Fact]
        public void SelectorEnvelopeSplitsFinalVirtualLeafWithoutMutation()
        {
            string json = BuildEnvelope(OwnerDrawSelectorCodec.Encode(
                PowerBuilderGridSelector()));

            OwnerDrawResult<OwnerDrawReplayRequest> first
                = OwnerDrawSelectorEnvelope.TrySplit(json);
            OwnerDrawResult<OwnerDrawReplayRequest> second
                = OwnerDrawSelectorEnvelope.TrySplit(json);

            Assert.True(first.IsSuccess, first.Status);
            Assert.True(second.IsSuccess, second.Status);
            Assert.Equal(first.Value.HostSelectorJson,
                second.Value.HostSelectorJson);
            Assert.DoesNotContain(OwnerDrawSelectorCodec.Marker,
                first.Value.HostSelectorJson);
            Assert.Contains(OwnerDrawSelectorCodec.Marker, json);
        }

        [Fact]
        public void MisplacedOrDuplicateOwnerDrawLeafFailsClosed()
        {
            List<OwnerDrawSelectorProperty> leaf = new List<OwnerDrawSelectorProperty>(
                OwnerDrawSelectorCodec.Encode(PowerBuilderGridSelector()));
            string duplicate = BuildEnvelope(leaf, duplicateLeaf: true);
            string misplaced = BuildEnvelope(leaf, ownerDrawFirst: true);

            Assert.Equal(OwnerDrawProviderState.RecognizedButFailed,
                OwnerDrawSelectorEnvelope.TrySplit(duplicate).State);
            Assert.Equal(OwnerDrawProviderState.RecognizedButFailed,
                OwnerDrawSelectorEnvelope.TrySplit(misplaced).State);
        }

        [Fact]
        public void DisabledOwnerDrawLeafFailsClosed()
        {
            string json = BuildEnvelope(OwnerDrawSelectorCodec.Encode(
                PowerBuilderGridSelector()), leafAccurate: false);

            OwnerDrawResult<OwnerDrawReplayRequest> result
                = OwnerDrawSelectorEnvelope.TrySplit(json);

            Assert.Equal(OwnerDrawProviderState.RecognizedButFailed,
                result.State);
            Assert.Equal(OwnerDrawFailureCode.InvalidSelector, result.Code);
        }

        [Fact]
        public void SelectorAdapterPreservesDisabledOwnerDrawLeafAtHead()
        {
            string selector = BuildEnvelope(OwnerDrawSelectorCodec.Encode(
                PowerBuilderGridSelector()), ownerDrawFirst: true,
                leafAccurate: false);
            var envelope = new JObject
            {
                [SelectorAdapter.COMMON_PROP_TAG] = new JObject
                {
                    [SelectorAdapter.VERSTION_TAG] = "1.3",
                },
                [II.RPA.Plugin.Windows.ConstFields.SELECTOR_ELEMENT_SELECTOR_TAG]
                    = JArray.Parse(selector),
            };

            ISelectorContext context = new SelectorAdapter().GetSelectorJson(
                new GetElementFromSelectorParams
                {
                    Selector = envelope.ToString(Formatting.None),
                    isActive = false,
                });

            Assert.Contains(OwnerDrawSelectorCodec.Marker, context.Selector);
            Assert.Equal(OwnerDrawProviderState.RecognizedButFailed,
                OwnerDrawSelectorEnvelope.TrySplit(context.Selector).State);
        }

        [Theory]
        [InlineData("owner-draw", "false")]
        [InlineData("native-provider", "damaged")]
        [InlineData("owner-draw-adapter", "")]
        [InlineData("owner-draw-adapter", "unknown-adapter")]
        [InlineData("owner-draw-version", "unknown")]
        public void SelectorAdapterPreservesDamagedReservedProperty(
            string propertyName, string propertyValue)
        {
            string selector = BuildEnvelope(new[]
            {
                new OwnerDrawSelectorProperty(propertyName, propertyValue, false),
            }, leafAccurate: false);
            var envelope = new JObject
            {
                [SelectorAdapter.COMMON_PROP_TAG] = new JObject
                {
                    [SelectorAdapter.VERSTION_TAG] = "1.3",
                },
                [II.RPA.Plugin.Windows.ConstFields.SELECTOR_ELEMENT_SELECTOR_TAG]
                    = JArray.Parse(selector),
            };

            ISelectorContext context = new SelectorAdapter().GetSelectorJson(
                new GetElementFromSelectorParams
                {
                    Selector = envelope.ToString(Formatting.None),
                    isActive = false,
                });

            Assert.Contains(propertyName, context.Selector,
                StringComparison.OrdinalIgnoreCase);
            Assert.Equal(OwnerDrawProviderState.RecognizedButFailed,
                OwnerDrawSelectorEnvelope.TrySplit(context.Selector).State);
        }

        [Fact]
        public void SelectorAdapterKeepsDisabledReservedPropertyForStrictDecode()
        {
            List<OwnerDrawSelectorProperty> properties = new List<OwnerDrawSelectorProperty>(
                OwnerDrawSelectorCodec.Encode(PowerBuilderGridSelector()));
            int markerIndex = properties.FindIndex(property =>
                property.Name == OwnerDrawSelectorCodec.Marker);
            properties[markerIndex] = new OwnerDrawSelectorProperty(
                OwnerDrawSelectorCodec.Marker, "true", false);
            string selector = BuildEnvelope(properties);
            var envelope = new JObject
            {
                [SelectorAdapter.COMMON_PROP_TAG] = new JObject
                {
                    [SelectorAdapter.VERSTION_TAG] = "1.3",
                },
                [II.RPA.Plugin.Windows.ConstFields.SELECTOR_ELEMENT_SELECTOR_TAG]
                    = JArray.Parse(selector),
            };

            ISelectorContext context = new SelectorAdapter().GetSelectorJson(
                new GetElementFromSelectorParams
                {
                    Selector = envelope.ToString(Formatting.None),
                    isActive = false,
                });

            Assert.Equal(OwnerDrawProviderState.RecognizedButFailed,
                OwnerDrawSelectorEnvelope.TrySplit(context.Selector).State);
        }

        [Fact]
        public void OwnerDrawSelectorAlwaysRequiresLiveResolutionInsteadOfCache()
        {
            string valid = BuildEnvelope(OwnerDrawSelectorCodec.Encode(
                PowerBuilderGridSelector()));
            string damaged = BuildEnvelope(new[]
            {
                new OwnerDrawSelectorProperty(
                    OwnerDrawSelectorCodec.Marker, "false", false),
            }, leafAccurate: false);
            string ordinary = BuildEnvelope(new[]
            {
                new OwnerDrawSelectorProperty("name", "ordinary", true),
            });

            Assert.True(GuiOperator.RequiresLiveOwnerDrawResolution(valid));
            Assert.True(GuiOperator.RequiresLiveOwnerDrawResolution(damaged));
            Assert.False(GuiOperator.RequiresLiveOwnerDrawResolution(ordinary));
        }

        [Fact]
        public void HostSelectorReuseRequiresSameHwndAndRuntimeIdentity()
        {
            var host = new IntPtr(1234);
            int[] runtimeId = { 42, 7, 9 };

            Assert.True(GuiOperator.CanReuseOwnerDrawHostSelector(host,
                runtimeId, host, new[] { 42, 7, 9 }));
            Assert.False(GuiOperator.CanReuseOwnerDrawHostSelector(
                new IntPtr(4321), runtimeId, host, runtimeId));
            Assert.False(GuiOperator.CanReuseOwnerDrawHostSelector(host,
                new[] { 42, 7, 10 }, host, runtimeId));
            Assert.False(GuiOperator.CanReuseOwnerDrawHostSelector(host,
                null, host, runtimeId));
        }

        [Fact]
        public void MouseOffsetMustRemainInsideResolvedCell()
        {
            var bounds = new Rectangle(100, 200, 20, 10);

            Assert.True(OwnerDrawVirtualElementNode.TryGetMouseTarget(bounds,
                MouseParamDef.CUR_POS_CENTER, 9, 4, out Point inside));
            Assert.Equal(new Point(119, 209), inside);
            Assert.False(OwnerDrawVirtualElementNode.TryGetMouseTarget(bounds,
                MouseParamDef.CUR_POS_CENTER, 10, 0, out _));
            Assert.False(OwnerDrawVirtualElementNode.TryGetMouseTarget(bounds,
                MouseParamDef.CUR_POS_LEFT_TOP, -1, 0, out _));
            Assert.False(OwnerDrawVirtualElementNode.TryGetMouseTarget(
                Rectangle.Empty, MouseParamDef.CUR_POS_CENTER, 0, 0, out _));
        }

        [Fact]
        public void RegistryIsolatesPickExceptionAsRecognizedFailure()
        {
            var registry = new OwnerDrawAdapterRegistry(new[]
            {
                new ThrowingAdapter(),
            });

            OwnerDrawResult<OwnerDrawElementSnapshot> result = registry.TryPick(
                new Point(1, 2), IntPtr.Zero);

            Assert.Equal(OwnerDrawProviderState.RecognizedButFailed,
                result.State);
            Assert.Contains("exception isolated", result.Status);
        }

        [Fact]
        public void RegistryNeverConvertsExplicitNotApplicableToHostSuccess()
        {
            var registry = new OwnerDrawAdapterRegistry(new[]
            {
                new NotApplicableAdapter(),
            });

            OwnerDrawResult<OwnerDrawElementSnapshot> result = registry.Resolve(
                new IntPtr(42), PowerBuilderGridSelector());

            Assert.Equal(OwnerDrawProviderState.RecognizedButFailed,
                result.State);
            Assert.Equal(OwnerDrawFailureCode.NotFound, result.Code);
        }

        [Fact]
        public void VclPingRejectsArchitectureMismatchAndMissingFlags()
        {
            // Expected architecture is the target's architecture, not the test/RPA
            // process architecture. This is essential for x64 -> x86 PB routing and
            // keeps the PING identity check deterministic in both test runners.
            string valid = Ping("x86");
            var validClient = new VclGridBridgeClient(
                new ConstantTransport(valid));
            Assert.True(validClient.TryQueryCapabilities(7,
                OwnerDrawArchitecture.X86,
                out VclGridBridgeClient.BridgeCapabilities capabilities,
                out string validStatus), validStatus);
            Assert.True(capabilities.SupportsLogicalCellResolve);
            Assert.True(capabilities.GdiPlusText);
            Assert.False(capabilities.FullLogicalModel);

            string wrongArchitecture = Ping("x64");
            var wrongClient = new VclGridBridgeClient(
                new ConstantTransport(wrongArchitecture));
            Assert.False(wrongClient.TryQueryCapabilities(7,
                OwnerDrawArchitecture.X86,
                out _, out string wrongStatus));
            Assert.Contains("expectedArch", wrongStatus);

            var missingClient = new VclGridBridgeClient(
                new ConstantTransport(valid.Replace("\toffscreenRead=0", "")));
            Assert.False(missingClient.TryQueryCapabilities(7,
                OwnerDrawArchitecture.X86,
                out _, out string missingStatus));
            Assert.Contains("offscreenRead", missingStatus);
        }

        [Fact]
        public void PowerBuilderPingRequiresAbiBuildAdapterArchitectureAndRunning()
        {
            const string architecture = "x86";
            string valid = "OK\tabi=" + PowerBuilderGdiBridgeClient.RequiredAbi
                + "\tbuildId=" + NativeOwnerDrawBridgeActivator
                    .RequiredPowerBuilderBridgeBuildId
                + "\tadapter=powerbuilder-datawindow\tarch=" + architecture
                + "\trunning=1\tstatus64=cG9uZw==";
            var validTransport = new ConstantTransport(valid);
            var validClient = new PowerBuilderGdiBridgeClient(validTransport);

            Assert.True(validClient.TryPing(17, OwnerDrawArchitecture.X86,
                out string validStatus), validStatus);
            Assert.Equal("PING\tv=" + PowerBuilderGdiBridgeClient.RequiredAbi,
                validTransport.LastRequest);

            var wrongAbiClient = new PowerBuilderGdiBridgeClient(
                new ConstantTransport(valid.Replace(
                    "abi=" + PowerBuilderGdiBridgeClient.RequiredAbi,
                    "abi=" + (PowerBuilderGdiBridgeClient.RequiredAbi - 1))));
            Assert.False(wrongAbiClient.TryPing(17,
                OwnerDrawArchitecture.X86, out _));

            var wrongBuildClient = new PowerBuilderGdiBridgeClient(
                new ConstantTransport(valid.Replace(
                    "buildId=" + NativeOwnerDrawBridgeActivator
                        .RequiredPowerBuilderBridgeBuildId,
                    "buildId=" + (NativeOwnerDrawBridgeActivator
                        .RequiredPowerBuilderBridgeBuildId - 1))));
            Assert.False(wrongBuildClient.TryPing(17,
                OwnerDrawArchitecture.X86, out string wrongBuildStatus));
            Assert.Contains("requiredBuildId", wrongBuildStatus);

            var wrongAdapterClient = new PowerBuilderGdiBridgeClient(
                new ConstantTransport(valid.Replace("powerbuilder-datawindow", "vcl-grid")));
            Assert.False(wrongAdapterClient.TryPing(17,
                OwnerDrawArchitecture.X86, out _));

            var wrongArchitectureClient = new PowerBuilderGdiBridgeClient(
                new ConstantTransport(valid.Replace("arch=" + architecture,
                    "arch=" + (architecture == "x64" ? "x86" : "x64"))));
            Assert.False(wrongArchitectureClient.TryPing(17,
                OwnerDrawArchitecture.X86, out _));

            var stoppedClient = new PowerBuilderGdiBridgeClient(
                new ConstantTransport(valid.Replace("running=1", "running=0")));
            Assert.False(stoppedClient.TryPing(17,
                OwnerDrawArchitecture.X86, out _));

            var missingBuildClient = new PowerBuilderGdiBridgeClient(
                new ConstantTransport(valid.Replace("\tbuildId="
                    + NativeOwnerDrawBridgeActivator
                        .RequiredPowerBuilderBridgeBuildId, string.Empty)));
            Assert.False(missingBuildClient.TryPing(17,
                OwnerDrawArchitecture.X86, out _));
        }

        [Fact]
        public void PowerBuilderRequestsAndResponsesStayOnAbiSix()
        {
            int oldAbi = PowerBuilderGdiBridgeClient.RequiredAbi - 1;
            var refreshTransport = new ConstantTransport(
                "OK_REFRESH\tabi=" + oldAbi + "\tviews=1");
            var refreshClient = new PowerBuilderGdiBridgeClient(refreshTransport);
            Assert.False(refreshClient.TryRefresh(17, new IntPtr(99), out _));
            Assert.Equal("REFRESH\tv=" + PowerBuilderGdiBridgeClient.RequiredAbi
                + "\thwnd=99",
                refreshTransport.LastRequest);

            var cellTransport = new ConstantTransport("OK_CELL\tabi=" + oldAbi
                + "\tindex=1\tkind=grid-cell\trow=1\tcol=2"
                + "\tscreenLeft=10\tscreenTop=20\tscreenWidth=30"
                + "\tscreenHeight=40\tclientLeft=1\tclientTop=2"
                + "\tclientWidth=30\tclientHeight=40"
                + "\ttext64=\tstatus64=cG9uZw==");
            var cellClient = new PowerBuilderGdiBridgeClient(cellTransport);
            Assert.False(cellClient.TryGetCellAt(17, new IntPtr(99),
                new Point(15, 25), out _, out _));
            Assert.StartsWith("GET_TEXT_AT\tv="
                + PowerBuilderGdiBridgeClient.RequiredAbi + "\t",
                cellTransport.LastRequest);
        }

        [Fact]
        public void PowerBuilderResolveRefreshesOnlyTheRequestedDataWindow()
        {
            var transport = new SequenceTransport(
                "OK_REFRESH\tabi=" + PowerBuilderGdiBridgeClient.RequiredAbi
                    + "\tviews=1",
                PowerBuilderCellResponse(7));
            var client = new PowerBuilderGdiBridgeClient(transport);
            OwnerDrawSelector selector = PowerBuilderGridSelector();

            Assert.True(client.TryResolveCell(17, new IntPtr(99), selector,
                out PowerBuilderGdiBridgeClient.BridgeCell cell,
                out OwnerDrawFailureCode failureCode, out string status), status);

            Assert.Equal(7, cell.Index);
            Assert.Equal("grid-cell", cell.Kind);
            Assert.Equal(3, cell.Row);
            Assert.Equal(2, cell.Col);
            Assert.Equal("最大欠款", cell.Field);
            Assert.Equal(OwnerDrawFailureCode.None, failureCode);
            Assert.Equal(2, transport.Requests.Count);
            Assert.Equal("REFRESH\tv=" + PowerBuilderGdiBridgeClient.RequiredAbi
                + "\thwnd=99", transport.Requests[0]);
            Assert.Equal("RESOLVE_CELL\tv="
                + PowerBuilderGdiBridgeClient.RequiredAbi
                + "\thwnd=99\tkind=grid-cell\trow=3\tcol=2",
                transport.Requests[1]);
        }

        [Fact]
        public void PowerBuilderTargetRefreshRequiresExactlyOneRefreshedView()
        {
            string prefix = "OK_REFRESH\tabi="
                + PowerBuilderGdiBridgeClient.RequiredAbi;
            var zeroClient = new PowerBuilderGdiBridgeClient(
                new ConstantTransport(prefix + "\tviews=0"));
            Assert.False(zeroClient.TryRefresh(17, new IntPtr(99),
                out string zeroStatus));
            Assert.Contains("views=0", zeroStatus);

            var missingClient = new PowerBuilderGdiBridgeClient(
                new ConstantTransport(prefix));
            Assert.False(missingClient.TryRefresh(17, new IntPtr(99),
                out string missingStatus));
            Assert.Contains("views=-1", missingStatus);

            var oneClient = new PowerBuilderGdiBridgeClient(
                new ConstantTransport(prefix + "\tviews=1"));
            Assert.True(oneClient.TryRefresh(17, new IntPtr(99),
                out string oneStatus), oneStatus);
        }

        [Fact]
        public void PowerBuilderResolveAcceptsOnlyDeclaredGuardedFallback()
        {
            // The resolver accepts a cell only when its kind/row/col/field match the
            // selector. A matching kind-based response is accepted; a mismatched one
            // is rejected as an identity mismatch.
            var matchTransport = new SequenceTransport(
                "OK_REFRESH\tabi=" + PowerBuilderGdiBridgeClient.RequiredAbi
                    + "\tviews=1",
                PowerBuilderCellResponse(7));
            var matchClient = new PowerBuilderGdiBridgeClient(matchTransport);
            OwnerDrawSelector selector = PowerBuilderGridSelector();

            Assert.True(matchClient.TryResolveCell(17, new IntPtr(99), selector,
                out PowerBuilderGdiBridgeClient.BridgeCell cell,
                out OwnerDrawFailureCode matchCode, out string matchStatus),
                matchStatus);
            Assert.Equal(7, cell.Index);
            Assert.Equal("grid-cell", cell.Kind);
            Assert.Equal(3, cell.Row);
            Assert.Equal(2, cell.Col);
            Assert.Equal("最大欠款", cell.Field);
            Assert.Equal(OwnerDrawFailureCode.None, matchCode);

            var mismatchTransport = new SequenceTransport(
                "OK_REFRESH\tabi=" + PowerBuilderGdiBridgeClient.RequiredAbi
                    + "\tviews=1",
                PowerBuilderCellResponse(9, row: 9));
            var mismatchClient = new PowerBuilderGdiBridgeClient(mismatchTransport);
            Assert.False(mismatchClient.TryResolveCell(17, new IntPtr(99), selector,
                out _, out OwnerDrawFailureCode mismatchCode,
                out string mismatchStatus));
            Assert.Equal(OwnerDrawFailureCode.IdentityMismatch, mismatchCode);
            Assert.Contains("row/col guard", mismatchStatus);
        }

        [Theory]
        [InlineData("invalid_request", (int)OwnerDrawFailureCode.InvalidRequest)]
        [InlineData("not_found", (int)OwnerDrawFailureCode.NotFound)]
        [InlineData("identity_mismatch", (int)OwnerDrawFailureCode.IdentityMismatch)]
        [InlineData("ambiguous", (int)OwnerDrawFailureCode.Ambiguous)]
        [InlineData("unknown", (int)OwnerDrawFailureCode.ProtocolMismatch)]
        public void PowerBuilderResolvePreservesNativeSemanticFailure(
            string nativeCode, int expectedCode)
        {
            var transport = new SequenceTransport(
                "OK_REFRESH\tabi=" + PowerBuilderGdiBridgeClient.RequiredAbi
                    + "\tviews=1",
                "ERR\tabi=" + PowerBuilderGdiBridgeClient.RequiredAbi
                    + "\tcode=" + nativeCode + "\tstatus64=ZmFpbA==");
            var client = new PowerBuilderGdiBridgeClient(transport);
            OwnerDrawSelector selector = PowerBuilderGridSelector();

            Assert.False(client.TryResolveCell(17, new IntPtr(99),
                selector, out _, out OwnerDrawFailureCode actualCode,
                out string status));
            Assert.Equal((OwnerDrawFailureCode)expectedCode, actualCode);
            Assert.Contains(nativeCode, status);
        }

        [Theory]
        [InlineData(0)] // grid-cell
        [InlineData(1)] // input
        [InlineData(2)] // label
        public void PowerBuilderSelectorRoundTripsPerKind(int kind)
        {
            OwnerDrawSelector original = kind == 0
                ? PowerBuilderGridSelector()
                : kind == 1 ? PowerBuilderInputSelector()
                : PowerBuilderLabelSelector();
            IReadOnlyList<OwnerDrawSelectorProperty> properties
                = OwnerDrawSelectorCodec.Encode(original);

            Assert.True(OwnerDrawSelectorCodec.TryDecode(properties,
                out OwnerDrawSelector decoded, out string status), status);
            Assert.Equal(OwnerDrawIdentityStrength.LogicalCell,
                decoded.IdentityStrength);
            Assert.Equal(original.PbKind, decoded.PbKind);

            Assert.Contains(properties, property =>
                property.Name == OwnerDrawSelectorCodec.PbKind
                && property.Accurate);

            if (original.PbKind == PowerBuilderCellKind.GridCell)
            {
                Assert.Equal(original.PbRow, decoded.PbRow);
                Assert.Equal(original.PbCol, decoded.PbCol);
                Assert.Equal(original.PbField, decoded.PbField);
                Assert.False(decoded.EnforceText);
            }
            else if (original.PbKind == PowerBuilderCellKind.Input)
            {
                Assert.Equal(original.CellClientBounds, decoded.CellClientBounds);
                Assert.False(decoded.EnforceText);
            }
            else
            {
                Assert.True(decoded.EnforceText);
                Assert.Equal("最大欠款", decoded.ExpectedText);
            }
        }

        [Fact]
        public void PowerBuilderLabelWithoutExactTextIsRejected()
        {
            OwnerDrawSelector selector = PowerBuilderLabelSelector();
            selector.EnforceText = false;

            Assert.False(OwnerDrawSelectorCodec.TryValidate(selector,
                out string status));
            Assert.Contains("exact text", status);
        }

        [Fact]
        public void PowerBuilderGridCellWithoutRowOrColIsRejected()
        {
            OwnerDrawSelector selector = PowerBuilderGridSelector();
            selector.PbCol = -1;

            Assert.False(OwnerDrawSelectorCodec.TryValidate(selector,
                out string status));
            Assert.Contains("row/col", status);
        }

        [Fact]
        public void PowerBuilderHeaderGridCellRowMinusOneIsValid()
        {
            OwnerDrawSelector selector = PowerBuilderGridSelector();
            selector.PbRow = -1; // header row is a valid logical row

            Assert.True(OwnerDrawSelectorCodec.TryValidate(selector,
                out string status), status);
        }

        private static string PowerBuilderCellResponse(int index,
            string kind = "grid-cell", int row = 3, int col = 2,
            string field = "最大欠款")
        {
            return "OK_CELL\tabi=" + PowerBuilderGdiBridgeClient.RequiredAbi
                + "\tindex=" + index + "\tkind=" + kind + "\trow=" + row
                + "\tcol=" + col + "\tfield64="
                + Convert.ToBase64String(Encoding.UTF8.GetBytes(field))
                + "\tscreenLeft=10\tscreenTop=20\tscreenWidth=30"
                + "\tscreenHeight=40\tclientLeft=1\tclientTop=2"
                + "\tclientWidth=30\tclientHeight=40"
                + "\ttext64=5a6i5oi3\tstatus64=cG9uZw==";
        }

        private static OwnerDrawSelector PowerBuilderGridSelector()
        {
            return new OwnerDrawSelector
            {
                SchemaVersion = OwnerDrawSelectorCodec.CurrentSchemaVersion,
                AdapterId = OwnerDrawSelectorCodec.PowerBuilderAdapterId,
                HostClassName = "pbdw80",
                IdentityStrength = OwnerDrawIdentityStrength.LogicalCell,
                PbKind = PowerBuilderCellKind.GridCell,
                PbRow = 3,
                PbCol = 2,
                PbField = "最大欠款",
                ExpectedText = "内蒙古海拉尔教育（不合作）书店",
                EnforceText = false,
            };
        }

        private static OwnerDrawSelector PowerBuilderInputSelector()
        {
            return new OwnerDrawSelector
            {
                SchemaVersion = OwnerDrawSelectorCodec.CurrentSchemaVersion,
                AdapterId = OwnerDrawSelectorCodec.PowerBuilderAdapterId,
                HostClassName = "pbdw80",
                IdentityStrength = OwnerDrawIdentityStrength.LogicalCell,
                PbKind = PowerBuilderCellKind.Input,
                CellClientBounds = new Rectangle(11, 12, 130, 20),
                ExpectedText = string.Empty,
                EnforceText = false,
            };
        }

        private static OwnerDrawSelector PowerBuilderLabelSelector()
        {
            return new OwnerDrawSelector
            {
                SchemaVersion = OwnerDrawSelectorCodec.CurrentSchemaVersion,
                AdapterId = OwnerDrawSelectorCodec.PowerBuilderAdapterId,
                HostClassName = "pbdw80",
                IdentityStrength = OwnerDrawIdentityStrength.LogicalCell,
                PbKind = PowerBuilderCellKind.Label,
                ExpectedText = "最大欠款",
                EnforceText = true,
            };
        }

        private static string BuildEnvelope(
            IReadOnlyList<OwnerDrawSelectorProperty> properties,
            bool duplicateLeaf = false, bool ownerDrawFirst = false,
            bool leafAccurate = true)
        {
            var host = new SelectorProps
            {
                Name = "Pane",
                Type = "6",
                Accurate = "true",
                Props = new List<Props>
                {
                    new Props
                    {
                        Name = "class",
                        Value = "pbdw80",
                        Pattern = "equal",
                        Accurate = "true",
                    },
                },
            };
            var leaf = new SelectorProps
            {
                Name = "Cell",
                Type = "6",
                Accurate = leafAccurate ? "true" : "false",
                Props = new List<Props>(),
            };
            foreach (OwnerDrawSelectorProperty property in properties)
            {
                leaf.Props.Add(new Props
                {
                    Name = property.Name,
                    Value = property.Value,
                    Pattern = "equal",
                    Accurate = property.Accurate ? "true" : "false",
                });
            }

            var nodes = ownerDrawFirst
                ? new List<SelectorProps> { leaf, host }
                : new List<SelectorProps> { host, leaf };
            if (duplicateLeaf) nodes.Add(leaf);
            return JsonConvert.SerializeObject(nodes);
        }

        private static string Ping(string architecture)
        {
            return "OK\tabi=" + VclGridBridgeClient.RequiredAbi
                + "\tadapter=vcl-grid\tarch=" + architecture
                + "\trunning=1\tgdiVisibleCell=1\tgdiPlusText=1\tvisibleTable=0"
                + "\texactHeaderScan=1\trecordScrollResolve=1\trttiHelper=0"
                + "\tlogicalMetadata=0\tlogicalCellRead=0\tlogicalTable=0"
                + "\toffscreenRead=0\tfullLogicalModel=0\ttargetDependent=1"
                + "\tstatus64=cG9uZw==";
        }

        private sealed class ConstantTransport : IOwnerDrawBridgeTransport
        {
            private readonly string response;

            internal ConstantTransport(string response)
            {
                this.response = response;
            }

            internal string LastRequest { get; private set; }

            public bool TryRoundTrip(string pipePrefix, int processId,
                string request, int connectTimeoutMs, int readTimeoutMs,
                int maximumResponseBytes, out string actualResponse,
                out string status)
            {
                LastRequest = request;
                actualResponse = response;
                status = "stub";
                return true;
            }
        }

        private sealed class SequenceTransport : IOwnerDrawBridgeTransport
        {
            private readonly Queue<string> responses;

            internal SequenceTransport(params string[] responses)
            {
                this.responses = new Queue<string>(responses);
            }

            internal List<string> Requests { get; } = new List<string>();

            public bool TryRoundTrip(string pipePrefix, int processId,
                string request, int connectTimeoutMs, int readTimeoutMs,
                int maximumResponseBytes, out string actualResponse,
                out string status)
            {
                Requests.Add(request);
                if (responses.Count == 0)
                {
                    actualResponse = string.Empty;
                    status = "No scripted response is available.";
                    return false;
                }
                actualResponse = responses.Dequeue();
                status = "stub";
                return true;
            }
        }

        private sealed class ThrowingAdapter : OwnerDrawAdapterBase
        {
            public override string AdapterId => "throwing";
            public override int Priority => 1;
            public override bool TryLocateHost(Point screenPoint,
                IntPtr candidateHwnd, out IntPtr hostHwnd, out string status)
            {
                hostHwnd = IntPtr.Zero;
                status = string.Empty;
                return false;
            }
            public override OwnerDrawResult<OwnerDrawElementSnapshot> Pick(
                OwnerDrawPickRequest request)
            {
                throw new InvalidOperationException("test");
            }
            public override OwnerDrawResult<OwnerDrawElementSnapshot> Resolve(
                IntPtr hostHwnd, OwnerDrawSelector selector)
            {
                return OwnerDrawResult<OwnerDrawElementSnapshot>.NotApplicable();
            }
        }

        private sealed class NotApplicableAdapter : OwnerDrawAdapterBase
        {
            public override string AdapterId
                => OwnerDrawSelectorCodec.PowerBuilderAdapterId;
            public override int Priority => 1;
            public override bool TryLocateHost(Point screenPoint,
                IntPtr candidateHwnd, out IntPtr hostHwnd, out string status)
            {
                hostHwnd = IntPtr.Zero;
                status = string.Empty;
                return false;
            }
            public override OwnerDrawResult<OwnerDrawElementSnapshot> Pick(
                OwnerDrawPickRequest request)
            {
                return OwnerDrawResult<OwnerDrawElementSnapshot>.NotApplicable();
            }
            public override OwnerDrawResult<OwnerDrawElementSnapshot> Resolve(
                IntPtr hostHwnd, OwnerDrawSelector selector)
            {
                return OwnerDrawResult<OwnerDrawElementSnapshot>.NotApplicable();
            }
        }
    }
}
