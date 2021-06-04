using System;
using System.Collections.Generic;
using System.Linq;

namespace SCMM.Steam.Data.Models.Extensions
{
    public static class SteamTagsExtensions
    {
        public static string GetItemType(this IDictionary<string, string> itemTags, string itemName)
        {
            var itemType = (string)null;
            if (itemTags == null)
            {
                return itemType;
            }
            if (itemTags.ContainsKey(Constants.SteamAssetTagSkin))
            {
                itemType = Uri.EscapeDataString(
                    itemTags[Constants.SteamAssetTagSkin]
                );
            }
            if (itemTags.ContainsKey(Constants.SteamAssetTagItemType))
            {
                itemType = Uri.EscapeDataString(
                    itemTags[Constants.SteamAssetTagItemType]
                );
            }
            else if (itemTags.ContainsKey(Constants.SteamAssetTagWorkshop))
            {
                itemType = Uri.EscapeDataString(
                    itemTags.FirstOrDefault(x => x.Key.StartsWith(Constants.SteamAssetTagWorkshop)).Value
                );
            }
            else if (itemTags.ContainsKey(Constants.SteamAssetTagCategory))
            {
                itemType = Uri.EscapeDataString(
                    itemTags.FirstOrDefault(x => x.Key.StartsWith(Constants.SteamAssetTagCategory)).Value
                );
            }
            else if (!string.IsNullOrEmpty(itemName))
            {
                /*
                "door.hinged.metal": {
                    "localized_name": "Sheet Metal Door",
                    "matches": "113"
                },
                "rifle.ak": {
                    "localized_name": "AK47u",
                    "matches": "111"
                },
                "box.wooden.large": {
                    "localized_name": "Large Wooden Box",
                    "matches": "108"
                },
                "hoodie": {
                    "localized_name": "Hoodie",
                    "matches": "88"
                },
                "metal.facemask": {
                    "localized_name": "Metal Facemask",
                    "matches": "88"
                },
                "pants": {
                    "localized_name": "Pants",
                    "matches": "79"
                },
                "hatchet": {
                    "localized_name": "Hatchet",
                    "matches": "67"
                },
                "roadsign.kilt": {
                    "localized_name": "Roadsign Kilt",
                    "matches": "62"
                },
                "metal.plate.torso": {
                    "localized_name": "Metal Torso Plate",
                    "matches": "59"
                },
                "rifle.semiauto": {
                    "localized_name": "Semi Auto Rifle",
                    "matches": "59"
                },
                "roadsign.jacket": {
                    "localized_name": "Roadsign Jacket",
                    "matches": "59"
                },
                "coffeecan.helmet": {
                    "localized_name": "Coffeecan Helmet",
                    "matches": "53"
                },
                "sleepingbag": {
                    "localized_name": "Sleeping Bag",
                    "matches": "52"
                },
                "pistol.semiauto": {
                    "localized_name": "Semi Auto Pistol",
                    "matches": "49"
                },
                "crossbow": {
                    "localized_name": "Crossbow",
                    "matches": "39"
                },
                "shoes.boots": {
                    "localized_name": "Boots",
                    "matches": "38"
                },
                "burlap.gloves": {
                    "localized_name": "Burlap Gloves",
                    "matches": "37"
                },
                "pistol.revolver": {
                    "localized_name": "Revolver",
                    "matches": "37"
                },
                "door.hinged.wood": {
                    "localized_name": "Wooden Door",
                    "matches": "36"
                },
                "shotgun.double": {
                    "localized_name": "Double Barrel Shotgun",
                    "matches": "36"
                },
                "stonehatchet": {
                    "localized_name": "Stone Hatchet",
                    "matches": "35"
                },
                "rock": {
                    "localized_name": "Rock",
                    "matches": "33"
                },
                "smg.2": {
                    "localized_name": "SMG",
                    "matches": "28"
                },
                "door.hinged.toptier": {
                    "localized_name": "Armored Metal Door",
                    "matches": "27"
                },
                "hammer": {
                    "localized_name": "Hammer",
                    "matches": "23"
                },
                "rifle.bolt": {
                    "localized_name": "Bolt Rifle",
                    "matches": "23"
                },
                "salvaged.sword": {
                    "localized_name": "Salvaged Sword",
                    "matches": "23"
                },
                "shotgun.pump": {
                    "localized_name": "Pump Shotgun",
                    "matches": "23"
                },
                "mask.balaclava": {
                    "localized_name": "Balaclava",
                    "matches": "22"
                },
                "stone.pickaxe": {
                    "localized_name": "Stone Pickaxe",
                    "matches": "22"
                },
                "smg.mp5": {
                    "localized_name": "Mp5",
                    "matches": "21"
                },
                "tshirt": {
                    "localized_name": "TShirt",
                    "matches": "21"
                },
                "mask.bandana": {
                    "localized_name": "Bandana",
                    "matches": "20"
                },
                "box.wooden": {
                    "localized_name": "Wooden Box",
                    "matches": "19"
                },
                "burlap.shirt": {
                    "localized_name": "Burlap Shirt",
                    "matches": "19"
                },
                "shotgun.waterpipe": {
                    "localized_name": "Waterpipe Shotgun",
                    "matches": "19"
                },
                "tshirt.long": {
                    "localized_name": "Long TShirt",
                    "matches": "17"
                },
                "rocket.launcher": {
                    "localized_name": "Rocket Launcher",
                    "matches": "16"
                },
                "riot.helmet": {
                    "localized_name": "Rifle Helmet",
                    "matches": "15"
                },
                "bucket.helmet": {
                    "localized_name": "Bucket Helmet",
                    "matches": "12"
                },
                "explosive.satchel": {
                    "localized_name": "Satchel Explosives",
                    "matches": "12"
                },
                "burlap.trousers": {
                    "localized_name": "Burlap Trousers",
                    "matches": "11"
                },
                "hat.cap": {
                    "localized_name": "Cap",
                    "matches": "11"
                },
                "jacket": {
                    "localized_name": "Jacket",
                    "matches": "11"
                },
                "hat.boonie": {
                    "localized_name": "Boonie",
                    "matches": "10"
                },
                "burlap.headwrap": {
                    "localized_name": "Burlap Headwrap",
                    "matches": "8"
                },
                "icepick.salvaged": {
                    "localized_name": "Salvaged Icepick",
                    "matches": "8"
                },
                "hat.beenie": {
                    "localized_name": "Beenie",
                    "matches": "7"
                },
                "bone.club": {
                    "localized_name": "Bone Club",
                    "matches": "6"
                },
                "jacket.snow": {
                    "localized_name": "Snow Jacket",
                    "matches": "6"
                },
                "longsword": {
                    "localized_name": "Longsword",
                    "matches": "6"
                },
                "pants.shorts": {
                    "localized_name": "Shorts",
                    "matches": "6"
                },
                "shirt.collared": {
                    "localized_name": "Collared Shirt",
                    "matches": "6"
                },
                "smg.thompson": {
                    "localized_name": "Thompson",
                    "matches": "6"
                },
                "attire.hide.poncho": {
                    "localized_name": "Hide Poncho",
                    "matches": "5"
                },
                "barricade.concrete": {
                    "localized_name": "Concrete Barricade",
                    "matches": "5"
                },
                "knife.bone": {
                    "localized_name": "Bone Knife",
                    "matches": "5"
                },
                "deer.skull.mask": {
                    "localized_name": "Deer Skull Mask",
                    "matches": "4"
                },
                "fun.guitar": {
                    "localized_name": "Guitar",
                    "matches": "4"
                },
                "grenade.f1": {
                    "localized_name": "Grenade",
                    "matches": "4"
                },
                "attire.hide.boots": {
                    "localized_name": "Boots",
                    "matches": "3"
                },
                "hat.miner": {
                    "localized_name": "Miner's Hat",
                    "matches": "3"
                },
                "attire.hide.helterneck": {
                    "localized_name": "Hide Halterneck",
                    "matches": "2"
                },
                "attire.hide.pants": {
                    "localized_name": "Hide Pants",
                    "matches": "2"
                },
                "attire.hide.skirt": {
                    "localized_name": "Hide Skirt",
                    "matches": "2"
                },
                "burlap.shoes": {
                    "localized_name": "Burlap Shoes",
                    "matches": "2"
                },
                "target.reactive": {
                    "localized_name": "Reactive Sign",
                    "matches": "2"
                },
                "barricade.sandbags": {
                    "localized_name": "Sandbag Barricade",
                    "matches": "1"
                },
                "shirt.tanktop": {
                    "localized_name": "Tank Top",
                    "matches": "1"
                }
                 */
            }

            return Uri.UnescapeDataString(itemType ?? string.Empty);
        }
    }
}
