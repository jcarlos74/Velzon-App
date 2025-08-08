using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

using Microsoft.Extensions.DependencyInjection;

namespace DapperExt
{
    public static class ServiceCollectionExtensions
    {
        public static void AddDapperDbContext(this IServiceCollection services, string connectionString, DbProviderFactory factory )
        {
            //cria a instância do DapperDbContext sempre que for solicitado
            services.AddScoped<IDapperDbContext>(p => new DapperDbContext(connectionString, factory));

        }
    }
}
