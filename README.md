# Simple Private Room Hopper

A simple BepInEx plugin for Gorilla Tag that automatically hops through private rooms.

## Features

- **Simple Toggle**: One-click enable/disable private room hopping
- **3-Second Room Timer**: Stays in each room for exactly 3 seconds
- **5-Second Leave Timer**: Waits 5 seconds after leaving before joining next room
- **Room Management**: Add, remove, and manage private room codes
- **Smart Error Handling**: Automatically skips failed rooms for 30 seconds
- **Real-time Status**: Live countdown timers and status updates
- **Manual Controls**: JOIN buttons and JOIN NEXT functionality

## How to Use

1. **Add Room Codes**: Use the text field and ADD button to add private room codes
2. **Enable Hopper**: Click the "PRIVATE ROOM HOPPER: OFF" button to enable
3. **Automatic Hopping**: The plugin will automatically cycle through your rooms
4. **Monitor Status**: Watch the countdown timers and status messages

## Installation

1. Build the project using `dotnet build --configuration Release`
2. The plugin will be automatically deployed to the BepInEx plugins directory
3. Launch Gorilla Tag and the plugin will load automatically

## Behavior

- **Join room** → Start 3-second countdown
- **After 3 seconds** → Leave room automatically  
- **After leaving** → Start 5-second countdown
- **After 5 seconds** → Try to join next room
- **Repeat** → Continuous cycling through all rooms

## Status Messages

- **"Leaving room in: X.Xs"** - Countdown when in a room (3.0s → 0.0s)
- **"Next room in: X.Xs"** - Countdown after leaving a room (5.0s → 0.0s)
- **"Trying to join room..."** - When attempting to join
- **"Status: 3s in room, 5s between rooms"** - When active

## Requirements

- Gorilla Tag
- BepInEx 5.4.23.2 or later
- .NET Framework 4.8

## Build

```bash
dotnet build --configuration Release
```

The compiled plugin will be automatically placed in the BepInEx plugins directory.

