//
// Copyright (C) Microsoft Corporation.  All rights reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace MyFixIt.Persistence
{
    public class FixItQueueManager : IFixItQueueManager
    {
        private CloudQueueClient _queueClient;
        private IFixItTaskRepository _repository;

        private static readonly string fixitQueueName = "fixits";

        public FixItQueueManager(IFixItTaskRepository repository)
        {
            _repository = repository;
            CloudStorageAccount storageAccount = StorageUtils.StorageAccount;
            _queueClient = storageAccount.CreateCloudQueueClient();
        }

        // Puts a serialized fixit onto the queue.
        public async Task SendMessageAsync(FixItTask fixIt)
        {
            CloudQueue queue = _queueClient.GetQueueReference(fixitQueueName);
            await queue.CreateIfNotExistsAsync();

            var fixitJson = JsonConvert.SerializeObject(fixIt);
            CloudQueueMessage message = new CloudQueueMessage(fixitJson);

            await queue.AddMessageAsync(message);
        }

        // Processes any messages on the queue.
        public async Task ProcessMessagesAsync()
        {
            CloudQueue queue = _queueClient.GetQueueReference(fixitQueueName);
            await queue.CreateIfNotExistsAsync();

            while (true)
            {
                CloudQueueMessage message = await queue.GetMessageAsync();
                if (message == null)
                {
                    break;
                }
                FixItTask fixit = JsonConvert.DeserializeObject<FixItTask>(message.AsString);
                await _repository.CreateAsync(fixit);
                await queue.DeleteMessageAsync(message);
            }
        }
    }
}
