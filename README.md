# Foreword - Postvideo
At long last it's here [video](https://youtu.be/pIMKLOHAcbk), please read the entire readme before downloading or using the plugin

# Contributors or would be contributors
I'm forced to put this project on the back burner for a bit, there are a couple of key critical changes I will make, namely getting rid of the gRPC service entirely and the "PlayerJoin" jank I wrote after rewritting the parsing code for the 50th time.
If you wish to contribute, please join the discord or dm directly before doing so, please do not submit fully AI written PR without even reviewing the code.
# LoggingWay

LoggingWay is a platform that aim to provide a different competitive experience when it comes to FFXIV raiding.
This repository contains the main plugin required to parse,upload logs, and view leaderboards from LoggingWay.
LoggingWay is not and does not aim to be a complete replacement of FFLogs.

LoggingWay require an [xivauth](https://xivauth.net/) account to log in, you should create one before attempting to log-in.

# Scope of Data collection
LoggingWay is currently only interested in self data collection, this means that while for parsing reasons it must processes messages from every players, only the player signed in and with their characters registered via xivauth will be saved
and used for ranking purposes, others players are completely disregarded at this stage, this might change in the future as the platform matures, but is how things are right now.
# Word of Caution

LoggingWay should be considered in very early developement stages, while the platform is operational, nothing is final, the ranking,UI,codebase,scopes should be considered Early prototype/Alpha stages.

This means don't get attached to any rankings you might obtain, or the way things are, as everything is subject to changes, your data might be deleted at anypoint for future versions


## Support
Either open an issue here or join us in [Discord](https://discord.gg/apKPyWpZm8).



## Installing 

1. `/xlsettings` -> Experimental tab
2. Copy and paste the repo.json link below
3. Click on the + button
4. Click on the "Save and Close" button
5. You will now see LoggingWayPlugin listed in the Available Plugins tab in the Dalamud Plugin Installer
6. Do not forget to actually install LoggingWayPlugin from this tab.

Do not install by manually downloading a release and putting it in your dev plugin.
- https://raw.githubusercontent.com/LoggingWay/LoggingWayPlugin/main/repo.json
