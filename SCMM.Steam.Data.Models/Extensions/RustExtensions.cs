namespace SCMM.Steam.Data.Models.Extensions
{
    public static class RustExtensions
    {
        public static string ToRustItemId(this string itemType)
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
                case "LR300": return "lr300.item";
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
                default: return null;
            }
        }

        public static bool IsRustAttireItem(this string itemType)
        {
            switch (itemType)
            {
                case "Bandana Mask": 
                case "Baseball Cap":
                case "Beenie Hat": 
                case "Bone Helmet": 
                case "Boonie Hat": 
                case "Boots": 
                case "Bucket Helmet": 
                case "Burlap Headwrap": 
                case "Burlap Shirt": 
                case "Burlap Shoes": 
                case "Burlap Trousers":
                case "Coffee Can Helmet":
                case "Hide Boots":
                case "Hide Halterneck":
                case "Hide Pants": 
                case "Hide Poncho": 
                case "Hide Skirt":
                case "Hide Vest": 
                case "Hoodie":
                case "Improvised Balaclava": 
                case "Jacket":
                case "Leather Gloves":
                case "Longsleeve T-Shirt": 
                case "Metal Chest Plate":
                case "Metal Facemask":
                case "Miners Hat":
                case "Pants":
                case "Riot Helmet": 
                case "Road Sign Jacket": 
                case "Road Sign Kilt": 
                case "Roadsign Gloves": 
                case "Shirt": 
                case "Shorts": 
                case "Snow Jacket": 
                case "Tank Top": 
                case "T-Shirt": return true;
                default: return false;
            }
        }

        public static bool IsRustWeaponItem(this string itemType)
        {
            switch (itemType)
            {
                case "Assault Rifle": 
                case "Bolt Action Rifle": 
                case "Crossbow":
                case "Custom SMG": 
                case "Double Barrel Shotgun": 
                case "Eoka Pistol": 
                case "Hunting Bow":
                case "L96 Rifle":
                case "LR300":
                case "M249": 
                case "M39 Rifle":
                case "MP5A4": 
                case "Pump Shotgun": 
                case "Python Revolver":
                case "Revolver": 
                case "Rocket Launcher": 
                case "Semi-Automatic Pistol":
                case "Semi-Automatic Rifle": 
                case "Thompson": 
                case "Waterpipe Shotgun":
                case "F1 Grenade": 
                case "Satchel Charge":
                case "Bone Club": 
                case "Bone Knife": 
                case "Combat Knife": 
                case "Longsword": 
                case "Salvaged Sword": return true;
                default: return false;
            }
        }

        public static bool IsRustToolItem(this string itemType)
        {
            switch (itemType)
            {
                case "Hammer": 
                case "Hatchet": 
                case "Pickaxe": 
                case "Jackhammer": 
                case "Rock":
                case "Salvaged Icepick": 
                case "Stone Hatchet": 
                case "Stone Pickaxe": return true;
                default: return false;
            }
        }

        public static bool IsRustDeployableItem(this string itemType)
        {
            switch (itemType)
            {
                case "Armored Door": 
                case "Armored Double Door": 
                case "Garage Door":
                case "Chair": 
                case "Concrete Barricade": 
                case "Fridge":
                case "Furnace": 
                case "Large Wood Box":
                case "Locker": 
                case "Reactive Target":
                case "Rug": 
                case "Rug Bear Skin": 
                case "Sheet Metal Door": 
                case "Sheet Metal Double Door":
                case "Sandbag Barricade": 
                case "Sleeping Bag": 
                case "Table": 
                case "Vending Machine": 
                case "Water Purifier":
                case "Wood Double Door":
                case "Wood Storage Box": 
                case "Wooden Door": return true;
                default: return false;
            }
        }

        public static bool IsRustUniqueItem(this string itemType)
        {
            return (
                !itemType.IsRustAttireItem() &&
                !itemType.IsRustWeaponItem() &&
                !itemType.IsRustToolItem() &&
                !itemType.IsRustDeployableItem()
            );
        }
    }
}
