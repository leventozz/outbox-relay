namespace OutboxRelay.Common.Exceptions
{
    public abstract class RabbitMqException : Exception
    {
        protected RabbitMqException(string message) : base(message) { }
        protected RabbitMqException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class RabbitMqPublishException : RabbitMqException
    {
        public RabbitMqPublishException(string exchange, string routingKey)
            : base($"Failed to publish message to RabbitMQ. Exchange: '{exchange}', RoutingKey: '{routingKey}'.") { }

        public RabbitMqPublishException(string exchange, string routingKey, Exception innerException)
            : base($"Failed to publish message to RabbitMQ. Exchange: '{exchange}', RoutingKey: '{routingKey}'.", innerException) { }

        public RabbitMqPublishException(string message) : base(message) { }
    }

    public class RabbitMqTopologyException : RabbitMqException
    {
        public RabbitMqTopologyException(string exchange, string queue)
            : base($"Failed to setup RabbitMQ topology. Exchange: '{exchange}', Queue: '{queue}'.") { }

        public RabbitMqTopologyException(string exchange, string queue, Exception innerException)
            : base($"Failed to setup RabbitMQ topology. Exchange: '{exchange}', Queue: '{queue}'.", innerException) { }

        public RabbitMqTopologyException(string message) : base(message) { }
    }
}
