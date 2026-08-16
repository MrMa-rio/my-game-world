namespace MyGameWorld.Client.ActorRuntime
{
    public interface IActorController
    {
        bool IsBound { get; }
        void Bind(ActorContext context);
        void Unbind();
    }

    public abstract class ActorController : UnityEngine.MonoBehaviour, IActorController
    {
        public bool IsBound => Context != null;
        protected ActorContext Context { get; private set; }

        public void Bind(ActorContext context)
        {
            if (context == null) throw new System.ArgumentNullException(nameof(context));
            if (IsBound) throw new System.InvalidOperationException($"Controller {GetType().Name} is already bound.");
            Context = context;
            try { OnBound(); }
            catch { OnUnbinding(); Context = null; throw; }
        }

        public void Unbind()
        {
            if (!IsBound) return;
            try { OnUnbinding(); }
            finally { Context = null; }
        }

        protected abstract void OnBound();
        protected virtual void OnUnbinding() { }
        protected virtual void OnDestroy() => Unbind();
    }
}
