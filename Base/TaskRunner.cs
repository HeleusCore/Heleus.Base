using System;
using System.Threading.Tasks;

namespace Heleus.Base
{
    public static class TaskRunner
    {
        public static void Run(Func<Task> task)
        {
            Task.Run(async () =>
            {
                try
                {
                    await task.Invoke();
                }
                catch (Exception ex)
                {
                    Log.HandleException(ex);
                }

            });
        }

        public static void Run(Action action)
        {
            Task.Run(() =>
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Log.HandleException(ex);
                }
            });
        }

        public static void Run(Task task)
        {
            Task.Run(async () =>
            {
                try
                {
                    await task;
                }
                catch (Exception ex)
                {
                    Log.HandleException(ex);
                }
            });
        }
    }
}
