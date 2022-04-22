namespace SCMM.Steam.Data.Models.Extensions
{
    public static class RustExtensions
    {
        public static string ToRustItemShortName(this string itemType)
        {
            switch (itemType)
            {
                case "Acoustic Guitar": return "fun.guitar";
                case "Armored Door": return "door.hinged.toptier";
                case "Armored Double Door": return "door.double.hinged.toptier";
                case "Assault Rifle": return "rifle.ak";
                case "Bandana Mask": return "mask.bandana";
                case "Baseball Cap": return "hat.cap";
                case "Beenie Hat": return "hat.beenie";
                case "Bolt Action Rifle": return "rifle.bolt"; // texture issues (Tempered)
                case "Bone Club": return "bone.club";
                case "Bone Helmet": return "deer.skull.mask";
                case "Bone Knife": return "knife.bone";
                case "Boonie Hat": return "hat.boonie";
                case "Boots": return "shoes.boots";
                case "Bucket Helmet": return "bucket.helmet";
                case "Burlap Headwrap": return "burlap.headwrap";
                case "Burlap Shirt": return "burlap.shirt";
                case "Burlap Shoes": return "burlap.shoes";
                case "Burlap Trousers": return "burlap.trousers";
                case "Chair": return "chair";
                case "Coffee Can Helmet": return "coffeecan.helmet";
                case "Combat Knife": return "knife.combat";
                case "Concrete Barricade": return "barricade.concrete";
                case "Crossbow": return "crossbow"; // texture issues (Tempered)
                case "Custom SMG": return "smg.2"; // texture issues (Lovely SMG)
                case "Double Barrel Shotgun": return "shotgun.double"; // broken model
                case "Eoka Pistol": return "pistol.eoka";
                case "F1 Grenade": return "grenade.f1";
                case "Fridge": return "fridge"; // door is open
                case "Furnace": return "furnace";
                case "Garage Door": return "wall.frame.garagedoor";
                case "Hammer": return "hammer";
                case "Hatchet": return "hatchet";
                case "Hazmat Suit": return "hazmatsuit";
                case "Hide Boots": return "attire.hide.boots";
                case "Hide Halterneck": return "attire.hide.helterneck";
                case "Hide Pants": return "attire.hide.pants";
                case "Hide Poncho": return "attire.hide.poncho";
                case "Hide Skirt": return "attire.hide.skirt";
                case "Hide Vest": return "attire.hide.vest";
                case "Hoodie": return "hoodie";
                case "Hunting Bow": return "bow.hunting";
                case "Improvised Balaclava": return "mask.balaclava";
                case "Jacket": return "jacket";
                case "Jackhammer": return "jackhammer";
                case "L96 Rifle": return "rifle.l96";
                case "Large Wood Box": return "box.wooden.large"; // texture issues (Large Stickered Toy Car)
                case "Leather Gloves": return "burlap.gloves";
                case "Locker": return "locker"; // texture issues (Heli Cargo), doors are open
                case "Longsleeve T-Shirt": return "tshirt.long";
                case "Longsword": return "longsword";
                case "LR300": return "rifle.lr300";
                case "M249": return "lmg.m249"; // broken model
                case "M39 Rifle": return "rifle.m39";
                case "Metal Chest Plate": return "metal.plate.torso";
                case "Metal Facemask": return "metal.facemask";
                case "Miners Hat": return "hat.miner";
                case "MP5A4": return "smg.mp5";
                case "Pants": return "pants";
                case "Pickaxe": return "pickaxe";
                case "Pump Shotgun": return "shotgun.pump"; // can't hide bullet
                case "Python Revolver": return "pistol.python"; // can't hide chamber
                case "Reactive Target": return "target.reactive";
                case "Revolver": return "pistol.revolver";
                case "Riot Helmet": return "riot.helmet"; // broken model
                case "Road Sign Jacket": return "roadsign.jacket";
                case "Road Sign Kilt": return "roadsign.kilt";
                case "Roadsign Gloves": return "roadsign.gloves";
                case "Rock": return "rock";
                case "Rocket Launcher": return "rocket.launcher";
                case "Rug": return "rug";
                case "Rug Bear Skin": return "rug.bear";
                case "Salvaged Icepick": return "icepick.salvaged";
                case "Salvaged Sword": return "salvaged.sword";
                case "Sandbag Barricade": return "barricade.sandbags";
                case "Satchel Charge": return "explosive.satchel"; // broken model
                case "Semi-Automatic Pistol": return "pistol.semiauto";
                case "Semi-Automatic Rifle": return "rifle.semiauto";
                case "Sheet Metal Door": return "door.hinged.metal";
                case "Sheet Metal Double Door": return "door.double.hinged.metal";
                case "Shirt": return "shirt.collared"; // texture issues (torso should map to white)
                case "Shorts": return "pants.shorts";
                case "Sleeping Bag": return "sleepingbag";
                case "Snow Jacket": return "jacket.snow";
                case "Stone Hatchet": return "stonehatchet";
                case "Stone Pickaxe": return "stone.pickaxe";
                case "Table": return "table";
                case "Tank Top": return "shirt.tanktop";
                case "Thompson": return "smg.thompson";
                case "T-Shirt": return "tshirt";
                case "Vending Machine": return "vending.machine"; // broken model
                case "Water Purifier": return "water.purifier";
                case "Waterpipe Shotgun": return "shotgun.waterpipe";
                case "Wood Double Door": return "door.double.hinged.wood";
                case "Wood Storage Box": return "box.wooden"; // texture issues (Heli Cargo)
                case "Wooden Door": return "door.hinged.wood";
                default: return itemType;
            }
        }

        public static string ToRustItemGroup(this string itemType)
        {
            if (IsRustArmourItem(itemType))
            {
                return "Armour";
            }
            else if (IsRustClothingItem(itemType))
            {
                return "Clothing";
            }
            else if (IsRustGunItem(itemType))
            {
                return "Guns";
            }
            else if (IsRustWeaponItem(itemType))
            {
                return "Weapons";
            }
            else if (IsRustToolItem(itemType))
            {
                return "Tools";
            }
            else if (IsRustDoorItem(itemType))
            {
                return "Doors";
            }
            else if (IsRustDeployableItem(itemType))
            {
                return "Deployables";
            }
            else if (IsRustFunItem(itemType))
            {
                return "Fun";
            }
            return null;
        }

        public static bool IsRustArmourItem(this string itemType)
        {
            switch (itemType)
            {
                case "Metal Facemask":
                case "Metal Chest Plate":
                case "Coffee Can Helmet":
                case "Road Sign Jacket":
                case "Road Sign Kilt":
                case "Roadsign Gloves":
                case "Riot Helmet":
                case "Bucket Helmet":
                case "Bone Helmet": return true;
                default: return false;
            }
        }

        public static bool IsRustClothingItem(this string itemType)
        {
            switch (itemType)
            {
                case "Beenie Hat":
                case "Snow Jacket":
                case "Hoodie":
                case "Pants":
                case "Boots":
                case "Jacket":
                case "Leather Gloves":
                case "Longsleeve T-Shirt":
                case "Baseball Cap":
                case "Shirt":
                case "Boonie Hat":
                case "T-Shirt":
                case "Miners Hat":
                case "Bandana Mask":
                case "Improvised Balaclava":
                case "Tank Top":
                case "Shorts":
                case "Hazmat Suit":
                case "Hide Boots":
                case "Hide Halterneck":
                case "Hide Pants":
                case "Hide Poncho":
                case "Hide Skirt":
                case "Hide Vest":
                case "Burlap Headwrap":
                case "Burlap Shirt":
                case "Burlap Shoes":
                case "Burlap Trousers": return true;
                default: return false;
            }
        }

        public static bool IsRustGunItem(this string itemType)
        {
            switch (itemType)
            {
                case "M249":
                case "L96 Rifle":
                case "M39 Rifle":
                case "LR300":
                case "Bolt Action Rifle":
                case "Assault Rifle":
                case "MP5A4":
                case "Semi-Automatic Rifle":
                case "Thompson":
                case "Custom SMG":
                case "Semi-Automatic Pistol":
                case "Python Revolver":
                case "Pump Shotgun":
                case "Revolver":
                case "Double Barrel Shotgun":
                case "Waterpipe Shotgun":
                case "Eoka Pistol": return true;
                default: return false;
            }
        }

        public static bool IsRustWeaponItem(this string itemType)
        {
            switch (itemType)
            {
                case "Rocket Launcher":
                case "F1 Grenade":
                case "Satchel Charge":
                case "Crossbow":
                case "Hunting Bow":
                case "Longsword":
                case "Salvaged Sword":
                case "Combat Knife":
                case "Bone Knife":
                case "Bone Club": return true;
                default: return false;
            }
        }

        public static bool IsRustToolItem(this string itemType)
        {
            switch (itemType)
            {
                case "Hammer":
                case "Jackhammer":
                case "Salvaged Icepick":
                case "Pickaxe":
                case "Hatchet":
                case "Stone Pickaxe":
                case "Stone Hatchet":
                case "Rock": return true;
                default: return false;
            }
        }

        public static bool IsRustDoorItem(this string itemType)
        {
            switch (itemType)
            {
                case "Armored Double Door":
                case "Armored Door":
                case "Garage Door":
                case "Sheet Metal Double Door":
                case "Sheet Metal Door":
                case "Wood Double Door":
                case "Wooden Door": return true;
                default: return false;
            }
        }

        public static bool IsRustDeployableItem(this string itemType)
        {
            switch (itemType)
            {
                case "Vending Machine":
                case "Large Wood Box":
                case "Wood Storage Box":
                case "Sleeping Bag":
                case "Locker":
                case "Fridge":
                case "Furnace":
                case "Water Purifier":
                case "Table":
                case "Chair":
                case "Rug":
                case "Rug Bear Skin":
                case "Sandbag Barricade":
                case "Concrete Barricade":
                case "Reactive Target": return true;
                default: return false;
            }
        }

        public static bool IsRustFunItem(this string itemType)
        {
            switch (itemType)
            {
                case "Acoustic Guitar": return true;
                default: return false;
            }
        }

        public static bool IsRustSpecialItem(this string itemType)
        {
            return (
                !itemType.IsRustArmourItem() &&
                !itemType.IsRustClothingItem() &&
                !itemType.IsRustGunItem() &&
                !itemType.IsRustWeaponItem() &&
                !itemType.IsRustToolItem() &&
                !itemType.IsRustDoorItem() &&
                !itemType.IsRustDeployableItem() &&
                !itemType.IsRustFunItem()
            );
        }
    }
}
