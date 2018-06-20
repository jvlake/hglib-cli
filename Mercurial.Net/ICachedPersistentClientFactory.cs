namespace Mercurial
{
    /// <summary>
    /// Make one PersistentClient per repository path
    /// </summary>
    public interface ICachedPersistentClientFactory : IClientFactory
    {
        /// <summary>
        /// remove all clients
        /// </summary>
        void ClearAllClients();
    }


}