# Procedural art direction

The reference is the sheet attached in the chat. No local reference sheet, slicing script or cropped sprites are required.

`../Scripts/SpriteFactory.cs` draws deterministic pixel textures at 48 pixels per world unit. Navy/charcoal outlines, amber wood and lights, cold teal/blue metal, muted red clothing, orange helmets, green hoods and red creature eyes follow the supplied visual direction. Tiles have grain, seams, bevels and wear; objects use dark outlines and stepped highlights.

Every current sprite uses this factory. SpriteLibrary caches directional animation frames and can accept individual future sprites in Resources. The separate material under Resources preserves the built-in sprite shader in player builds. Runtime sprite textures use point filtering and no import compression.
