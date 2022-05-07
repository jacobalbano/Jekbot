using Jekbot.Utility;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot.Resources
{
    internal class Database : PreparableResource<Database, Database.Factory>, IDisposable
    {
        private bool disposedValue;

        public class Factory : IFactory<Database>
        {
            Database IFactory<Database>.Create() => new Database(DatabaseName);

            private const string DatabaseName = "jekbot.db";
        }

        private Database(string dbLocation)
        {
            database = new LiteDatabase(dbLocation);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    database.Dispose();
                    database = null;
                }

                disposedValue = true;
            }
        }

        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        LiteDatabase? database;
    }
}
