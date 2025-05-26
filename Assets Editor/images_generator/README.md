# Image generator for outfits and items
- Created by [Zbizu](https://github.com/Zbizu) - added more features, added an option to generate items by count/subtype, added an option to generate outfits by lookTypeEx
- [Based on the code of tools provided by Gesior](https://ots.me/)
- [GIF Creator library based on Sybio's repository](https://github.com/Sybio/GifCreator) (and modified by [Gesior](https://github.com/gesior))

# Installation
1. Select this folder as export path for "export as images" in asset editor
2. Add it to your website
3. Enable gd extension in your php.ini
4. restart/reload your web server

# Item examples
**The generator for item images supports only client ids!**

### 5x gold ingot
`/item.php?id=9045&count=5`

### bucket of water
`/item.php?id=2873&subtype=1`

### small sapphires (monster loot/wiki page style animation)
`/item.php?id=675&mode=loot`

### vial (wiki page style animation)
`/item.php?id=2874&mode=loot`

# Outfit examples

### full citizen outfit riding a colored mount, direction south, animated
`/outfit.php?type=128&head=10&body=20&legs=30&feet=40&addons=3&mount=1363&mounthead=50&mountbody=60&mountlegs=70&mountfeet=80&direction=2&animated=1&walk=1`

### full citizen outfit rotation in place (wiki page style)
`/outfit.php?type=128&head=78&body=68&legs=58&feet=76&addons=3&direction=2&walk=4&animated=1`

### full citizen outfit rotating and walking (wiki page style)
`/outfit.php?type=128&head=78&body=68&legs=58&feet=76&addons=3&direction=2&walk=3&animated=1`

### thundergiant facing south (no idle animation)
`/outfit.php?type=994&direction=2`

### thundergiant facing south (with idle animation)
`/outfit.php?type=994&direction=2&animated=1`

### thundergiant walking south
`/outfit.php?type=994&direction=2&walk=1&animated=1`

### thundergiant rotating in place (no idle animation)
`/outfit.php?type=994&direction=2&walk=4`

### thundergiant rotating in place (with idle animation)
`/outfit.php?type=994&direction=2&walk=4&animated=1`

### thundergiant rotating in and walking
`/outfit.php?type=994&direction=2&walk=3&animated=1`

### thundergiant rotating in place (with idle animation)
`/outfit.php?type=994&direction=2&walk=4&animated=1`

### 96x96 image of rotworm
`/outfit.php?type=26&direction=2&size=1`

### magicthrower
`/outfit.php?typeex=2190`
