# Foreword - Prevideo
I intend to make a video showcasing the reasoning/motive behind this project in more detail, for testing reasons I must make the plugin available, I am aware that people outside of those I explicitly invite might stumble upon it, I can't stop you from joining, but do not download this plugin
expecting a full experience out of the box. I am aware the codebase is shit you don't need to send me messages about it
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
