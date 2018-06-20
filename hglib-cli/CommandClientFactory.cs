using System;
using System.Collections.Generic;

namespace Mercurial
{
    public interface ICommandClientFactory
    {
        void ClearAllClients();
        CommandClient GetClient(string repositoryPath);
    }

    public class CommandClientFactory : ICommandClientFactory
    {
        private Dictionary<string, CommandClient> _clients;

        public CommandClientFactory()
        {
            this._clients = new Dictionary<string, CommandClient>();
        }

        public void ClearAllClients()
        {
            foreach (var c in _clients)
            {
                c.Value?.Dispose();
            }

            this._clients = new Dictionary<string, CommandClient>();
        }

        public CommandClient GetClient(string repositoryPath)
        {
            if (_clients.ContainsKey(repositoryPath)) return _clients[repositoryPath];
            _clients[repositoryPath] = new CommandClient(repositoryPath,String.Empty, null,null);
            return _clients[repositoryPath];
        }
    }
}
