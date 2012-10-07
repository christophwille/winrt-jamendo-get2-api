using System;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Station366.Common
{
    // http://mikaelkoskinen.net/post/winrt-invoke-method-in-ui-thread.aspx
    public static class UIDispatcher
    {
        private static CoreDispatcher dispatcher;

        public static void Initialize()
        {
            dispatcher = Window.Current.Dispatcher;
        }

        public static void BeginExecute(Action action)
        {
            if (dispatcher.HasThreadAccess)
                action();

            else dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => action());
        }

        public static void Execute(Action action)
        {
            InnerExecute(action).Wait();
        }

        private static async Task InnerExecute(Action action)
        {
            if (dispatcher.HasThreadAccess)
                action();

            else await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => action());
        }
    }
}