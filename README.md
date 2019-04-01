# lagom-dotnet
This is a .NET core port of Lightbend's Lagom framework using Akka.NET.  

## CI/CD

[![CircleCI](https://circleci.com/gh/nagytech/lagom-dotnet.svg?style=shield)](https://circleci.com/gh/nagytech/lagom-dotnet)

NuGet package: https://www.nuget.org/packages/wyvern.api

## Features

So far, the list of supported features are:

- REST API (with Swagger generation)
- Journal and Snapshot persistence (SQL Server)
- Read side implementation (SQL Server)
- Topics of event streams (via AMQP)

Additionally, the following features may be handy:

- Akka cluster visualizer

Things that have been skipped, not fully implemented, or may need rework:

- Entities can be partitioned, but Topics and Read Side do not support partitioning yet
- Serialization of messages is static
- Limited test coverage

## Getting started with the example

### Setup

- Install dotnetcore 2.1
- Install docker and docker-compose
- Update `docker-compose` and `./docker/db/Dockerfile` with an appropriate password for SQL Server
- Run `docker-compose up db amqp` to start the SQL Server and ActiveMQ containers
- Run `dotnet run` inside the `./example` folder

### Usage

- Open http://localhost:5000/ and use the Swagger UI to make several POST / GET calls
- To see websockets in action, open a connection to ws://localhost:5000/ws/hello
- To view the event journal, connect to the 'db' docker container and browse the db
- To see the event bus, browse to http://localhost:8161/admin/queues.jsp using `admin:admin`
