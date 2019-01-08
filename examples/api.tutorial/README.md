> Wyvern
> A .NET Core port of Lagom Framework

# Introduction

This is a walkthrough of how to start using wyvern. This folder contains the
end result of the tutorial - follow the instructions below from scratch to
see how it's all put together.

# Steps

## Create a new project

Create a new dotnet core web project and add the basic wyvern references:

1. mkdir my-project
2. dotnet new web
3. Add references to the .csproj file:
~~~~
  <ItemGroup>
    <PackageReference Include="Akka" Version="1.3.10" />
    <PackageReference Include="Akka.Streams" Version="1.3.10" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\relative-path-to\wyvern.api.csproj" />
    <ProjectReference Include="..\relative-path-to\wyvern.entity.csproj" />
  </ItemGroup>
~~~~

## Scaffold a basic entity

~~~~
create abstract command
~~~~

~~~~
create abstract event
~~~~

~~~~
create abstract state
~~~~

## Create the sharded entity

~~~~
create typed entity
~~~~

## Add Service Definition

## Add Service Implementation

## Register Command Handlers

## Register Event Handlers

## Register IoC Components

Make the following edits to Startup.cs:

1. include a reference to wyvern.api:

`using wyvern.api;`

# Configuration

# Running

## Swagger Generation

## Cluster Scaling

## Check the Database
