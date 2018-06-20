using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Mercurial
{
    /// <inheritdoc />
    public sealed class CachedPersistentClientFactory : ICachedPersistentClientFactory
    {
        private Dictionary<string, PersistentClient> _clients;

        /// <summary>
        /// Make one PersistentClient per repository path
        /// </summary>
        public CachedPersistentClientFactory()
        {
            this._clients = new Dictionary<string, PersistentClient>();
        }

        /// <inheritdoc />
        public void ClearAllClients()
        {
            try
            {
                foreach (var client in this._clients)
                {
                    client.Value?.Dispose();
                }
            }
            catch (Exception ex)
            {
                Debug.Write(ex);
            }

            this._clients = new Dictionary<string, PersistentClient>();
        }

        /// <inheritdoc />
        public IClient CreateClient(string repositoryPath)
        {
            if (_clients.ContainsKey(repositoryPath)) return _clients[repositoryPath];

            _clients[repositoryPath] = new PersistentClient(repositoryPath);

            return _clients[repositoryPath];
        }
    }
}