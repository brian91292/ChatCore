# ChatCore
ChatCore is a shared chat client library written in .NET Standard 2.0. The main objective behind this project is to reduce overhead in situations where multiple assemblies may want to interact with the same chat services (this is most useful with game modifications that have several significant chat integrations).

# Basic Configuration (for Beat Saber mod users)
1. Grab the latest ChatCore.dll and ChatCore.manifest from https://github.com/brian91292/ChatCore/releases
2. Copy ChatCore.dll into the `Libs` folder inside your `Beat Saber` directory.
3. Copy ChatCore.manifest into the `Plugins` folder inside your `Beat Saber` directory.
4. After installing any mod that utilizes ChatCore, a settings web app will be launched upon starting the game. Use this to login, join/leave channels, and configure various settings.


# Basic Project Setup (for devs)
Check out the included [Test Project](https://github.com/brian91292/ChatCore/blob/develop/ChatCoreTester/) for a basic example of how to start the ChatCore services.
