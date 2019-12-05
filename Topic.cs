using System;
using System.Threading.Tasks;
using SeraphinaNET.Data;

namespace SeraphinaNET {
    class TopicService {
        // At this point I'm beginning to ask myself if I should place my services in their own folder
        private readonly DataContextFactory data;

        public TopicService(DataContextFactory data) {
            this.data = data;
        }
    }
}
