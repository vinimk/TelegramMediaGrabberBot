# TelegramMediaGrabberBot
This bot once added will download tweets, medias(video/images) from various websites (including instagram, tiktok, youtube shorts, etc) and send to your group/chat, this removes the necessity of the users to leave the chat to another app to see this kind of content, this also fixes that a lot of websites have the preview broken, since the media will be sent fully. 
This project is only configured to work with docker, because of the dependencies on yt-dlp and ffmpeg, in theory it could be done without the docker image, but since the deploy is way easier and the developement inside the container works great with visual studio 2022, this is the way it is done.

#Usage and deploy
First, clone the repository in the machine you wanna deploy, create a new telegram bot with @BotFather on telegram, get the token and make sure your bot is allowed to see the messages in the group (privacy setting that has to be set with BotFather), after that, copy the token for the bot and put inside the appsettings.json in the correct place.
After that, you have to configure the WhiteListed in order to use the bot, the bot could work without the whitelist feature, but this feature is built-in because I don't want random people using my bot instance because of costs of running the bot.
So, get the id from the group/the user, the easiest way is using @myidbot, add it to the groups/send it a private message, get the id with the command /getid or /getgroupid, copy the ID and put it into the array in the appsettings.json, separete it with commas, save the file and you are good to go.
The bot will work by default with the domains in the SupportedWebSites tag, but any website that is supported by yt-dlp should work because the implementation is generic, so if you have any website that is supported by yt-dlp, just add it to the appsettings.json and it should work.
After that, your machine should have docker and docker-compose working, go to the directory that the cloned repo is, then you just run the commands:
Of course the any_name can be change to any name that you want
```
docker build -t any_name .
```
The image will be built, then simply run it:
```
docker run -d any_name
```

After that, the bot is up and running, any message that is sent to it or to a group it has access it will process it.
