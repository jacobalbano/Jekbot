# Jekbot
Personal discord bot running on my raspberry pi

# Build
```
PM> dotnet publish -r linux-arm -c Release
```

# Deploy
Copy modified files from /bin/Release/[...]/linux-arm/publish to install dir

# Todo
- Role-based command permissions
- Audit all messages the bot sends
- Versioning and migration system for models
- Make sure to catch exceptions from discord.net API
- Rolling log files per day
- Deployment script