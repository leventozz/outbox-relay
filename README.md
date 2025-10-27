# OutboxRelay

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![EF Core](https://img.shields.io/badge/EF_Core-9.0-purple?style=flat-square&logo=.net)](https://learn.microsoft.com/ef/core)
[![RabbitMQ](https://img.shields.io/badge/RabbitMQ-7.1.2-FF6600?style=flat-square&logo=rabbitmq)](https://www.rabbitmq.com/)
[![SQL Server](https://img.shields.io/badge/SQL_Server-2022-CC2927?style=flat-square&logo=microsoftsqlserver)](https://www.microsoft.com/sql-server)

OutboxRelay is a robust message relay system implementing the Outbox pattern to ensure reliable message delivery in distributed systems. It guarantees at-least-once delivery semantics while maintaining data consistency through atomic transactions.

## Features

- **Outbox Pattern Implementation**: Ensures reliable message delivery with transactional consistency
- **At-least-once Delivery**: Guarantees message delivery even in case of failures
- **Atomic Transactions**: Maintains data consistency across database and message operations
- **Concurrent Processing Safety**: Thread-safe message handling with SQL Server locks
- **Idempotent Consumer**: Handles duplicate messages gracefully through status checks
- **Smart Retry Mechanism**: Database-level exponential backoff for failed deliveries
- **Publisher/Consumer Architecture**: Separate worker services for publishing and consuming messages
- **Monitoring & Logging**: Comprehensive logging for tracking message flow and debugging

## Technology Stack

- **.NET 8**: Latest .NET runtime with improved performance and features
- **Entity Framework Core 9**: Modern ORM with code-first approach
- **SQL Server**: Robust database engine for storing transactions and outbox messages
- **RabbitMQ 7.1.2**: Message broker for reliable message delivery
- **Unit of Work Pattern**: Ensures transaction consistency
- **Repository Pattern**: Clean data access abstraction

## Architecture

The project implements the Outbox pattern to handle distributed transactions reliably, with several key safety mechanisms:

1. **Transaction Flow**:
   - Incoming transaction is saved to the database
   - Outbox message is created in the same transaction
   - Publisher service picks up pending messages
   - Messages are published to RabbitMQ
   - Consumer processes messages and updates transaction status

2. **Concurrency Safety**:
   - **Publisher Concurrency**: Uses SQL Server's `UPDLOCK` and `READPAST` hints in to prevent duplicate message processing when multiple publisher instances are running
   - **Consumer Concurrency**: Implements pessimistic locking with `UPDLOCK` in to prevent race conditions during message consumption
   - **Idempotent Consumer**: Handles duplicate messages through transaction status checks, ensuring at-least-once delivery without side effects

3. **Retry Management**:
   - Implements database-level exponential backoff
   - Prevents self-DDoS during service outages (e.g., RabbitMQ down)
   - Automatically adjusts retry intervals based on failure count

2. **Components**:
   - `OutboxRelay.Api`: REST API for transaction creation
   - `OutboxRelay.PublisherWorker`: Background service for message publishing
   - `OutboxRelay.ConsumerWorker`: Background service for message consuming
   - `OutboxRelay.Core`: Domain models and database context
   - `OutboxRelay.Infrastructure`: Data access and message broker implementation

## Installation

### Prerequisites

- .NET 8 SDK
- SQL Server 2022
- RabbitMQ 7.1.2

### Setup

1. **Clone the Repository**
```bash
git clone https://github.com/leventozz/OutboxRelay.git
cd OutboxRelay
```

2. **Configure Connection Strings**

Update `appsettings.json` in each project:

```json
{
  "ConnectionStrings": {
    "MSSQL": "Server=localhost;Database=OutboxRelay;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest"
  }
}
```

3. **Run the Projects**

```bash
# Start API
dotnet run --project OutboxRelay.Api

# Start Publisher Worker
dotnet run --project OutboxRelay.PublisherWorker

# Start Consumer Worker
dotnet run --project OutboxRelay.ConsumerWorker
```

The database will be automatically created on first run using EF Core's `EnsureCreated()`.

## Usage

### Creating a Transaction

```http
POST /api/transactions
Content-Type: application/json

{
    "fromAccountId": 1,
    "toAccountId": 2,
    "amount": 100.00
}
```

### Message Flow

1. Transaction is created via API
2. Outbox entry is created in the same transaction
3. Publisher worker picks up pending messages
4. Messages are published to RabbitMQ
5. Consumer worker processes messages and updates transaction status

## Project Structure

```
OutboxRelay/
├── OutboxRelay.Api/                 # REST API
├── OutboxRelay.Application/         # Application logic
├── OutboxRelay.Common/             # Shared components
├── OutboxRelay.ConsumerWorker/     # Message consumer
├── OutboxRelay.Core/               # Domain models
├── OutboxRelay.Infrastructure/     # Data access & messaging
└── OutboxRelay.PublisherWorker/   # Message publisher
```

### Key Components

- `AppDbContext`: EF Core database context implementing code-first approach with optimized table structures and indexes
- `IUnitOfWork`: Ensures transactional consistency across multiple operations with automatic rollback on failures
- `OutboxRepository`: Handles atomic message claiming using UPDLOCK/READPAST and implements database-level exponential backoff logic
- `RabbitMqPublisher`: Manages reliable message publishing with connection recovery and channel management
- `TransactionConsumedHandler`: Ensures idempotent message processing and handles consumer-side pessimistic locking to prevent race conditions

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

### Development Environment

- Visual Studio 2022 or later
- SQL Server Management Studio
- RabbitMQ Management Console

### Coding Standards

- Follow C# coding conventions
- Use async/await for I/O operations
- Implement proper exception handling
- Add appropriate logging
- Write unit tests for new features

## License

This project is licensed under the MIT License - see the LICENSE file for details.
