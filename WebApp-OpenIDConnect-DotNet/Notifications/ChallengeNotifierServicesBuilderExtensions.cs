using System;
using WebApp_OpenIDConnect_DotNet.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using Azure.Storage.Queues;

namespace WebApp_OpenIDConnect_DotNet
{
    public static class ChallengeNotifierServicesBuilderExtensions
    {
        public static void AddStorageQueueNotificationServices(this IServiceCollection services, string storageSas, string tenantId, bool resolve = true)
        {
            var validUri = Uri.TryCreate(storageSas, UriKind.Absolute, out var path);
            if (!validUri)
            {
                throw new ArgumentOutOfRangeException("storageSas");
            }

            if (resolve)
            {
                // this is only disposing here because we won't use it again and there isn't a clean way to swap handlers on the same httpclient instance
                // note the addition of ConnectionClose = true - this will kill the socket immediately after use instead of keeping it open - which is what we want here
                using var h = new HttpClient(new SocketsHttpHandler() { AllowAutoRedirect = false });
                h.DefaultRequestHeaders.ConnectionClose = true; // we don't need it after this call, so let's trash it and the socket immediately
                var request = h.GetAsync(path).Result;
                if (request.StatusCode == System.Net.HttpStatusCode.Moved || request.StatusCode == System.Net.HttpStatusCode.Redirect)
                {
                    path = request.Headers.Location;
                }
            }

            services.AddSingleton<IProgressNotificationService, StorageQueueProgressNotificationService>(x =>
            {
                var queueService = new QueueServiceClient(path);
                // if we include the queue name here and in the sas, we get a double path
                var queueClient = queueService.GetQueueClient(string.Empty); 
                return new StorageQueueProgressNotificationService(queueClient, tenantId);
            });
        }
    }
}
