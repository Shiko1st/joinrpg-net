Please direct any questions to @Leotsarev or leo@joinrpg.ru

How to build locally
==

For now joinrpg could be build & run only on **Windows**. Porting to .NET Core slow underway, but still not here.

1. Start with forking this repository on github. 
1. Download [Visual Studio 2019](https://www.visualstudio.com/). Community Edition should be enough.
1. Recommended to install Github Extension (options during install). See more on https://visualstudio.github.com/.
1. Download .Net Framework 4.7.2 and .Net Framework 4.7.2 SDK (targeting pack).
1. Input you github credentials into Visual Studio and clone your repository.
  - Or clone repository using Git cmd client or Github for windows or whatever you like.
1. Perform  
	- `git remote add upstream https://github.com/joinrpg/joinrpg-net.git`
	- `git fetch upstream`
1. Compile VS project and observe package restoring and everything happens
1. Tap F5 and you should be there.
1. First start can take a long time while Local DB will create database for first time.
    1. If you got error about "The argument 2 of the JSON_MODIFY should be string literal", you have outdated version of LocalDb. Updating it could be tricky, see [here](https://knowledge-base.havit.eu/2018/09/04/sql-localdb-upgrade-to-2017-14-0-1000/)
1. Some features will be disabled — it's all about integration to Remote systems — email, social services login etc. 
If you want to test this, you will need to rename Secret.config.example to Secret.config and provide correct keys.
1. First logged-in user will have his email automatically confirmed and adminstrator flag will be set. If you want more users, you have to change DB or setup mail integration.

Submitting your changes
==
1. 1 branch = 1 feature. 
1. Test your changes locally, etc.
1. Commit.
1. Rebase your changes on top of `upstream/master`
1. Open pull request via Github UI from your branch to `joinrpg/joirpg-net/master`

Telegram chats
==

We using a lot of tlgrm chats to talk about Joinrpg. Developer's question could be addressed to https://t.me/joinchat/AAAAAEKdDvRjI0Z1n79EJA сhat.
