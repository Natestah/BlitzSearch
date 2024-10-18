using System;
using System.Threading.Tasks;

namespace Blitz.Avalonia.Controls.ViewModels;


/// <summary>
/// Helper for long running background tasks that is not cancelable but may have a re-request and needs to re-reun.
/// </summary>
public class SingleTask
{
    private Task? _singleTask;
    private bool _needsReRun;
        
    public void Run()
    {
        lock (this)
        {
            if (_singleTask == null || _singleTask.IsCompleted || _singleTask.IsFaulted)
            {
                _singleTask = Task.Run(ExecuteOnce);
            }
            else
            {
                _needsReRun = true;
            }
        }
    }

    private void ExecuteOnce()
    {
        _taskAction.Invoke();
        lock (this)
        {
            if (_needsReRun)
            {
                _needsReRun = false;
                Task.Run(ExecuteOnce);
            }
        }
    }
        
    public SingleTask(Action action)
    {
        _taskAction = action;
    }
    private Action _taskAction;
}