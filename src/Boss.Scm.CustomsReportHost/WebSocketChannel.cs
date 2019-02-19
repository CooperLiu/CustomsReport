using System;
using System.Threading;
using NLog;
using WebSocketSharp;

namespace Boss.Scm.CustomsReportHost
{
    class WebSocketChannel
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();


        private readonly WebSocket _socket;

        private int _heartbeatInterval = 30000;//毫秒
        private ResetbaleTimer _webStockHeadBeatTimer;

        public bool IsConnected => _socket.ReadyState == WebSocketState.Open;

        //public ResetbaleTimer WebStockHeadBeatTimer
        //{
        //    get => _webStockHeadBeatTimer;
        //    set
        //    {
        //        _webStockHeadBeatTimer = value;
        //        _webStockHeadBeatTimer.Elapsed += (sender, e) =>
        //        {
        //            if (IsConnected)
        //            {
        //                _socket.Ping();
        //                _logger.Info($"WebStocket Ping, at {DateTime.Now}");
        //            }
        //        };
        //    }
        //}

        public WebSocketChannel(WebSocket socket)
        {
            _socket = socket;

            _webStockHeadBeatTimer = new ResetbaleTimer(_heartbeatInterval);
            _webStockHeadBeatTimer.Elapsed += (sender, e) =>
            {
                if (IsConnected)
                {
                    _socket.Ping();
                    _logger.Info($"WebStocket Ping, at {DateTime.Now}");
                }
            };

            _webStockHeadBeatTimer.Start();

        }

        public void Send(string message)
        {
            CheckChannelAlive();

            _socket.Send(message);
        }

        public void Close()
        {
            _socket.Close(CloseStatusCode.Normal);

            _webStockHeadBeatTimer?.WaitToStop();
            _webStockHeadBeatTimer = null;
        }

        private void CheckChannelAlive()
        {
            if (!IsConnected)
            {
                _logger.Info($"WebSocket closed");
                _webStockHeadBeatTimer?.Stop();
                throw new Exception("websocket channel is closed");
            }

            DelayCheckChannelAlive();
        }

        private void DelayCheckChannelAlive()
        {
            try
            {
                _webStockHeadBeatTimer?.Delay();
            }
            catch
            {
                // ignored
            }
        }



    }

    class ResetbaleTimer
    {

        /// <summary>
        /// This event is raised periodically according to Period of Timer.
        /// </summary>
        public event EventHandler Elapsed;

        /// <summary>
        /// Task period of timer (as milliseconds).
        /// </summary>
        public int Period { get; set; }

        /// <summary>
        /// Indicates whether timer raises Elapsed event on Start method of Timer for once.
        /// Default: False.
        /// </summary>
        public bool RunOnStart { get; set; }

        /// <summary>
        /// This timer is used to perfom the task at spesified intervals.
        /// </summary>
        private readonly Timer _taskTimer;

        /// <summary>
        /// Indicates that whether timer is running or stopped.
        /// </summary>
        private volatile bool _running;

        /// <summary>
        /// Indicates that whether performing the task or _taskTimer is in sleep mode.
        /// This field is used to wait executing tasks when stopping Timer.
        /// </summary>
        private volatile bool _performingTasks;

        /// <summary>
        /// Creates a new Timer.
        /// </summary>
        public ResetbaleTimer()
        {
            _taskTimer = new Timer(TimerCallBack, null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Creates a new Timer.
        /// </summary>
        /// <param name="period">Task period of timer (as milliseconds)</param>
        /// <param name="runOnStart">Indicates whether timer raises Elapsed event on Start method of Timer for once</param>
        public ResetbaleTimer(int period, bool runOnStart = false)
            : this()
        {
            Period = period;
            RunOnStart = runOnStart;
        }

        /// <summary>
        /// Starts the timer.
        /// </summary>
        public void Start()
        {
            if (Period <= 0)
            {
                throw new Exception("Period should be set before starting the timer!");
            }

            _running = true;
            _taskTimer.Change(RunOnStart ? 0 : Period, Timeout.Infinite);
        }

        /// <summary>
        /// Stops the timer.
        /// </summary>
        public void Stop()
        {
            lock (_taskTimer)
            {
                _running = false;
                _taskTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        public void Delay()
        {
            lock (_taskTimer)
            {
                _taskTimer.Change(Period, Period);
            }
        }

        /// <summary>
        /// Waits the service to stop.
        /// </summary>
        public void WaitToStop()
        {
            lock (_taskTimer)
            {
                while (_performingTasks)
                {
                    Monitor.Wait(_taskTimer);
                }
            }
        }

        /// <summary>
        /// This method is called by _taskTimer.
        /// </summary>
        /// <param name="state">Not used argument</param>
        private void TimerCallBack(object state)
        {
            lock (_taskTimer)
            {
                if (!_running || _performingTasks)
                {
                    return;
                }

                _taskTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _performingTasks = true;
            }

            try
            {
                if (Elapsed != null)
                {
                    Elapsed(this, new EventArgs());
                }
            }
            catch
            {

            }
            finally
            {
                lock (_taskTimer)
                {
                    _performingTasks = false;
                    if (_running)
                    {
                        _taskTimer.Change(Period, Timeout.Infinite);
                    }

                    Monitor.Pulse(_taskTimer);
                }
            }
        }
    }
}