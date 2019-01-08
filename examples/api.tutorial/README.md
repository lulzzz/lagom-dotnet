# wyvern - tutorial


## Introduction

This is a walkthrough of how to start using wyvern.

## Steps

Create the project and add basic references:

1. mkdir api.tutorial
2. dotnet new web
3. dotnet add reference ../wyvern.api/

## Startup

Make the following edits to Startup.cs:

TODO: Create template for web insetad of using default

1. include a reference to wyvern.api:

`using wyvern.api;`


## Scaffold a basic entity:

1. Install entity templates: `dotnet new -i ../wyvern.templates/Entity.CSharp`
# TODO: Change naming convention for find rep;lace.
2. dotnet new wyvern.entity --name BankAccount
