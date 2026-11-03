using II.RPA.Logging;
using II.RPA.Screenshot.Plugin.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Runtime.InteropServices;

namespace II.RPA.Screenshot.Interop
{
    /// <summary>
    /// Provides late-bound COM lookup helpers without .NET Framework Remoting proxies.
    /// </summary>
    public static class COMWrapper
    {
        private static readonly ILogger logger = Logger.OfContext();
        private const int MK_E_UNAVAILABLE = -2147221021;
        private const int CO_E_CLASSSTRING = -2147221005;

        [DllImport("ole32.dll")]
        private static extern int ProgIDFromCLSID([In] ref Guid clsid, [MarshalAs(UnmanagedType.LPWStr)] out string lplpszProgId);

        [DllImport("ole32.dll", CharSet = CharSet.Unicode)]
        private static extern int CLSIDFromProgID(string lpszProgID, out Guid lpclsid);

        [DllImport("oleaut32", PreserveSig = false)]
        private static extern void GetActiveObject(ref Guid rclsid, IntPtr pvReserved, [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);

        public static T GetInstance<T>()
        {
            Type type = ValidateComInterface<T>();
            var progIdAttribute = ComProgIdAttribute.GetAttribute(type);
            object comObject = GetActiveComObject(progIdAttribute.Value);
            return comObject is null ? default : (T)comObject;
        }

        public static T CreateInstance<T>()
        {
            Type type = ValidateComInterface<T>();
            var progIdAttribute = ComProgIdAttribute.GetAttribute(type);
            Type comType = GetComType(progIdAttribute.Value);

            if (comType == null)
            {
                return default;
            }

            try
            {
                object comObject = Activator.CreateInstance(comType);
                if (comObject != null)
                {
                    logger.LogDebug("Created new instance of {0} object.", progIdAttribute.Value);
                    return (T)comObject;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning("Error {1} creating object for {0}", progIdAttribute.Value, ex.Message);
                throw;
            }

            return default;
        }

        public static T GetOrCreateInstance<T>()
        {
            Type type = ValidateComInterface<T>();
            var progIdAttribute = ComProgIdAttribute.GetAttribute(type);

            object comObject = GetActiveComObject(progIdAttribute.Value);
            if (comObject != null)
            {
                return (T)comObject;
            }

            return CreateInstance<T>();
        }

        public static T Wrap<T>(object comObject)
        {
            if (comObject == null)
            {
                throw new ArgumentNullException(nameof(comObject));
            }

            ValidateComInterface<T>();
            return (T)comObject;
        }

        private static Type ValidateComInterface<T>()
        {
            Type type = typeof(T);
            if (!type.IsInterface)
            {
                throw new ArgumentException("The specified type must be an interface.", nameof(T));
            }

            var progIdAttribute = ComProgIdAttribute.GetAttribute(type);
            if (string.IsNullOrEmpty(progIdAttribute?.Value))
            {
                throw new ArgumentException("The specified type must define a ComProgId attribute.", nameof(T));
            }

            return type;
        }

        private static object GetActiveComObject(string progIdValue)
        {
            string progId = ResolveProgId(progIdValue, out Guid clsid);

            if (clsid != Guid.Empty && progId == progIdValue)
            {
                try
                {
                    GetActiveObject(ref clsid, IntPtr.Zero, out object comObject);
                    return comObject;
                }
                catch (Exception)
                {
                    logger.LogWarning("Error getting instance for class id {0}", progIdValue);
                    return null;
                }
            }

            int clsidResult = CLSIDFromProgID(progId, out Guid progIdClsid);
            if (clsidResult != 0)
            {
                logger.LogWarning("Unknown progId {0}", progId);
                return null;
            }

            try
            {
                GetActiveObject(ref progIdClsid, IntPtr.Zero, out object comObject);
                return comObject;
            }
            catch (COMException comEx) when (comEx.ErrorCode == MK_E_UNAVAILABLE)
            {
                logger.LogDebug("No current instance of {0} object available.", progId);
            }
            catch (Exception ex)
            {
                logger.LogWarning("Error getting active object for " + progId, ex);
            }

            return null;
        }

        private static Type GetComType(string progIdValue)
        {
            string progId = ResolveProgId(progIdValue, out Guid clsid);

            try
            {
                if (clsid != Guid.Empty && progId == progIdValue)
                {
                    return Type.GetTypeFromCLSID(clsid);
                }

                return Type.GetTypeFromProgID(progId, true);
            }
            catch (Exception ex)
            {
                logger.LogWarning("Error type for " + progIdValue, ex);
                return null;
            }
        }

        private static string ResolveProgId(string progIdValue, out Guid clsid)
        {
            clsid = Guid.Empty;
            string progId = progIdValue;

            if (progId.StartsWith("clsid:", StringComparison.OrdinalIgnoreCase))
            {
                clsid = new Guid(progId.Substring(6));
                int result = ProgIDFromCLSID(ref clsid, out progId);
                if (result != 0)
                {
                    logger.LogWarning("Error {0} getting progId {1}", result, progIdValue);
                    return progIdValue;
                }

                logger.LogInformation("Mapped {0} to progId {1}", progIdValue, progId);
            }

            return progId;
        }
    }
}
