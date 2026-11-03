using II.Framework.gRPC;
using II.Framework.Models;
using II.Framework.Utility;
using II.RPA.ZDeskTop.Abstracts;
using II.RPA.ZDeskTop.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace II.RPA.ZDeskTop.Services;

public class ControlService : IDisposable
{
    public IDisposable _disposable;
    public readonly ILogger _logger;
    public IControllableWindow _window;
    public readonly WorkerServiceProvider _workerServiceProvider;
    public WorkerConnection _workerConnection;
    private CompositeDisposable _disposables;
    private readonly Subject<bool> _runStream;
    private IObservable<ZdesktopActionMessage> _serverStream;
    public ControlService(WorkerServiceProvider serviceProvider, ILogger<ControlService> logger)
    {
        _logger = logger;
        _runStream = new Subject<bool>();
        _workerServiceProvider = serviceProvider;
        _disposables = new CompositeDisposable();
    }
    public IObservable<bool> RunStream => _runStream;
    public async Task SetWindow(IControllableWindow window)
    {
        _window = window;
        if (Global.IsSuperAdmin)
        {
            return;
        }
        await _workerServiceProvider.Connect();
        _workerConnection = _workerServiceProvider.Connection;
        _serverStream = _workerConnection.ServerStream;
        var operations = _workerConnection.ServerStream
            .Where(am =>
            {
                _logger.LogDebug($"Filtering action: {am.Action}");
                return am.Action == "zdesktopoperation";
            })
            .Do(m =>
            {
                _logger.LogDebug($"operations.{m.Action}:{m.RequestId}");
            })
            .Select(am =>
            {
                var message = JObject.Parse(am.Payload);
                return (HandleAction(message["action"].ToString(), message.GetValue("payload")),am);
            })
            .Subscribe(async actionWithMessage =>
            {
                actionWithMessage.Item1();
                var zdesktopMessage = new ZdesktopMessage
                {
                    Action = actionWithMessage.am.Action,
                    RequestId = actionWithMessage.am.RequestId,
                    ResponseFor = actionWithMessage.am.RequestId
                };
                var message = new ZdesktopActionMessage
                {
                    Action = zdesktopMessage.Action,
                    Payload = JsonConvert.SerializeObject(zdesktopMessage)
                };
                _logger.LogDebug($"Response for {actionWithMessage.am.Action}");
                await _workerConnection.Send(message);
            });
        var getzdesktopstatus = _workerConnection.ServerStream
            .Where(am => am.Action == "getzdesktopstatus")
            .Do(m =>
            {
                _logger.LogDebug($"getzdesktopstatus.{m.Action}:{m.RequestId}");
            })
            .Subscribe(async am =>
            {
                var rect = _window.Rect;
                var info = new ZdesktopInfoModel(rect, status: _window.Status, Mode.Normal, true, ZDeskTopWindow.Pid);
                await ReturnStatus(info, am.RequestId);
            });

        var updatetarsrunstatus = _workerConnection.ServerStream
            .Where(am => string.Equals(am.Action, "updatetarsrunstatus", StringComparison.OrdinalIgnoreCase))
            .Subscribe(am =>
            {
                if (am.PayloadObj.TryGetValue("buttonStatus", out var value))
                {
                    _logger.LogDebug($"buttonStatus:{value}");
                    var status = value.ToString();
                    if (status == "pause")
                    {
                        _runStream.OnNext(true);
                        return;
                    }
                }
                _runStream.OnNext(false);
            });
        var all = _serverStream
            .Subscribe(m =>
            {
                _logger.LogDebug($"all {m.Action}:{m.RequestId}");
            });

        var engine = _workerConnection.EngineStream.Subscribe(a => 
        {
            _logger.LogDebug($"EngineStream:{a}");
            if (a == "run")
            {
                _runStream.OnNext(true);
            }
            else if (a == "childrunover")
            {
                _runStream.OnNext(false);
            }
        });


        var rect = window.Rect;
        var info = new ZdesktopInfoModel(rect, status: Status.Starting, Mode.Normal, true, ZDeskTopWindow.Pid);
        _logger.LogDebug("Rx setup completely");
        await Task.Delay(100);
        await UpdateStatus(info);
        _disposables = new CompositeDisposable(operations, getzdesktopstatus, updatetarsrunstatus, engine, all);

    }
    public async Task Pause()
    {
        var zdesktopMessage = new ZdesktopMessage
        {
            Action = "zdesktoppause",
            RequestId = NewRequestId(),
        };
        var message = new ZdesktopActionMessage
        {
            Action = zdesktopMessage.Action,
            Payload = JsonConvert.SerializeObject(zdesktopMessage)
        };
        _logger.LogDebug($"send zdesktoppause:{zdesktopMessage.RequestId}");
        await _workerConnection.Send(message);
        await _serverStream.FirstAsync(m => m.RequestId == zdesktopMessage.RequestId)
            .Timeout(TimeSpan.FromSeconds(60))
            .ToTask();
        _logger.LogDebug("zdesktoppause over");
    }
    public async Task Resume()
    {
        var zdesktopMessage = new ZdesktopMessage
        {
            Action = "zdesktopresume",
            RequestId = NewRequestId(),
        };
        var message = new ZdesktopActionMessage
        {
            Action = zdesktopMessage.Action,
            Payload = JsonConvert.SerializeObject(zdesktopMessage)
        };
        await _workerConnection.Send(message);
        await _serverStream
            .FirstOrDefaultAsync(m => m.RequestId == zdesktopMessage.RequestId)
            .Timeout(TimeSpan.FromSeconds(30))
            .ToTask();
        _logger.LogDebug("zdesktopresume over");
    }
    public async void SetReady()
    {
        var rect = _window.Rect;
        var info = new ZdesktopInfoModel(rect, status: Status.Running, Mode.Normal, true, ZDeskTopWindow.Pid);
        await UpdateStatus(info);
    }
    public async Task UpdateStatus(ZdesktopInfoModel info)
    {
        if (Global.IsSuperAdmin)
        {
            return;
        }
        var zdesktopMessage = new ZdesktopMessage
        {
             Action = "updatezdesktopstatus",
             RequestId = NewRequestId(),
             PayloadJobject = JObject.FromObject(info),
        };
        var message = new ZdesktopActionMessage
        {
            Action = zdesktopMessage.Action,
            Payload = JsonConvert.SerializeObject(zdesktopMessage)
        };
        _logger.LogDebug($"UpdateStatus:{info}");
        await _workerConnection.SendWithRetry(message).ConfigureAwait(false);
    }
    public async Task ReturnStatus(ZdesktopInfoModel info,string requestId)
    {
        if (Global.IsSuperAdmin)
        {
            return;
        }
        var zdesktopMessage = new ZdesktopMessage
        {
            Action = "updatezdesktopstatus",
            RequestId = requestId,
            PayloadJobject = JObject.FromObject(info),
        };
        var message = new ZdesktopActionMessage
        {
            Action = zdesktopMessage.Action,
            Payload = JsonConvert.SerializeObject(zdesktopMessage)
        };
        _logger.LogDebug($"ReturnStatus:{info}");
        await _workerConnection.Send(message);
    }
    private Action HandleAction(string actionId, JToken args)
    {
        if (_window == null)
        {
            return () => 
            {
                _logger.LogWarning("window is not ready!");
            };
        }
        _logger.LogInformation($"HandleAction:{actionId}");
        return actionId switch
        {
            "maximize.on" => _window.Maximize,
            "minimize.on" => () => 
            {
                if (_window.IsMiniOn)
                {
                    _window.OverlayService.Disable();
                    _window.OverlayService.Hide();
                    _window.CancelMiniMode();
                }
                _window.Minimize();
            }
            ,
            "topmost.on" => () =>
                {
                    _window.SetTopmost(true);
                }
            ,
            "topmost.off" => () =>
                {
                    _window.SetTopmost(false);
                }

            ,
            "operation.on" => () =>
                {
                    _window.AllowOperation(true);
                }
            ,
            "operation.off" => () =>
                {
                    _window.AllowOperation(false);
                }
            ,
            "region.set" => () =>
                {
                    var rect = JsonConvert.DeserializeObject<JsonRect>(args.ToString());
                    _window.SetWindowRegion(rect.X, rect.Y, rect.Width, rect.Height);
                }
            ,
            "close" => () =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Application.Current.Shutdown();
                }, System.Windows.Threading.DispatcherPriority.Input);
            }
            ,
            "miniwindow.on" => () =>
            {
                var parentInfo = JsonConvert.DeserializeObject<ParentWindowModel>(args.ToString());
                _window.SetMiniMode(parentInfo);
                _window.OverlayService.Enable();
                _window.OverlayService.Show();
                _window.ActivateWindow();
            }
            ,
            "miniwindow.off" => ()=> 
            {
                _window.OverlayService.Disable();
                _window.OverlayService.Hide();
                _window.CancelMiniMode(); 
            }
            ,
            "hide.on" => () =>
                {
                    _window.PInvoker.HideWindow().Sleep(100);
                    if (_window.IsMiniOn)
                    {
                        _window.OverlayService.Disable();
                        _window.OverlayService.Hide();
                    }
                }
            ,
            "hide.off" => () =>
                {
                    _window.PInvoker.ShowWindow().Sleep(100);
                    if (_window.IsMiniOn)
                    {
                        _window.OverlayService.Enable();
                        _window.OverlayService.Show();
                    }
                    _window.ActivateWindow();
                }
            ,
            "subscribe.start" => () =>
                {
                    //_childSessionClient.SendingSnapshot(_window.Capture(100));
                }
            ,
            "subscribe.stop" => () =>
                {
                    //_childSessionClient.StopSendingSnapshot();
                }
            ,
            _ => () => { }
            ,
        };
    }
    public async void Dispose()
    {
        if (_window != null)
        {
            var rect = _window.Rect;
            var info = new ZdesktopInfoModel(rect, status: Status.Exited, Mode.Normal, true, ZDeskTopWindow.Pid);
            await UpdateStatus(info);
            await Task.Delay(500);
        }
        _disposable?.Dispose();
        _disposables?.Dispose();
        _workerServiceProvider?.DisConnect();
    }

    public static string NewRequestId()
    {
        var ticksOffset = DateTime.Now.Ticks - DateTime.MinValue.Ticks;
        var bytes = BitConverter.GetBytes(ticksOffset);
        Thread.Sleep(0);
        return BitConverter.ToString(bytes.Reverse().ToArray()).Replace("-", "");
    }
}
