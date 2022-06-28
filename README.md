# Jekbot
Personal discord bot running on my raspberry pi

# Build
```
PM> dotnet publish -r linux-arm -c Release
```

# Deploy
Copy modified files from /bin/Release/[...]/linux-arm/publish to install dir

# Todo
- Enable modules per-server
- Role-based command permissions
- Audit all messages the bot sends
