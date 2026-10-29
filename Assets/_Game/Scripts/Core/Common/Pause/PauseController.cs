using System.Collections.Generic;

namespace _Game.Scripts.Core.Common.Pause
{
    public class PauseController : IPauseController
    {
        private List<IPausable> _stoppables = new();

        public void Add(IPausable pausable)
            => _stoppables?.Add(pausable);

        public void Remove(IPausable pausable)
            => _stoppables?.Remove(pausable);

        public void PerformStop()
        {
            foreach (var item in _stoppables)
                item.Stop();
        }

        public void PerformResume()
        {
            foreach (var item in _stoppables)
                item.Resume();
        }

        public void Dispose()
        {
            _stoppables.Clear();
            _stoppables = null;
        }
    }
}