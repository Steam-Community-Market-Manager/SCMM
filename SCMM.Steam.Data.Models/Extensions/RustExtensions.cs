
namespace SCMM.Steam.Data.Models.Extensions
{
    public static class RustExtensions
    {
        public static string RustWorkshopTagToItemType(this string workshopTag)
        {
            switch (workshopTag)
            {
                case "Bandana": return "Bandana Mask";
                case "Balaclava": return "Improvised Balaclava";
                case "Beenie Hat": return "Beenie Hat";
                case "Burlap Shoes": return "Burlap Shoes";
                case "Burlap Shirt": return "Burlap Shirt";
                case "Burlap Pants": return "Burlap Trousers";
                case "Burlap Headwrap": return "Burlap Headwrap";
                case "Bucket Helmet": return "Bucket Helmet";
                case "Boonie Hat": return "Boonie Hat";
                case "Cap": return "Baseball Cap";
                case "Collared Shirt": return "Shirt";
                case "Coffee Can Helmet": return "Coffee Can Helmet";
                case "Deer Skull Mask": return "Bone Helmet";
                case "Hide Skirt": return "Hide Skirt";
                case "Hide Shirt": return "Hide Vest";
                case "Hide Pants": return "Hide Pants";
                case "Hide Shoes": return "Hide Boots";
                case "Hide Halterneck": return "Hide Halterneck";
                case "Hoodie": return "Hoodie";
                case "Hide Poncho": return "Hide Poncho";
                case "Leather Gloves": return "Leather Gloves";
                case "Long TShirt": return "Longsleeve T-Shirt";
                case "Metal Chest Plate": return "Metal Chest Plate";
                case "Metal Facemask": return "Metal Facemask";
                case "Miner Hat": return "Miners Hat";
                case "Pants": return "Pants";
                case "Roadsign Vest": return "Road Sign Jacket";
                case "Roadsign Pants": return "Road Sign Kilt";
                case "Riot Helmet": return "Riot Helmet";
                case "Snow Jacket": return "Snow Jacket";
                case "Shorts": return "Shorts";
                case "Tank Top": return "Tank Top";
                case "TShirt": return "T-Shirt";
                case "Vagabond Jacket": return "Jacket";
                case "Work Boots": return "Boots";
                case "AK47": return "Assault Rifle";
                case "Bolt Rifle": return "Bolt Action Rifle";
                case "Bone Club": return "Bone Club";
                case "Bone Knife": return "Bone Knife";
                case "Crossbow": return "Crossbow";
                case "Double Barrel Shotgun": return "Double Barrel Shotgun";
                case "Eoka Pistol": return "Eoka Pistol";
                case "F1 Grenade": return "F1 Grenade";
                case "Longsword": return "Longsword";
                case "Mp5": return "MP5A4";
                case "Pump Shotgun": return "Pump Shotgun";
                case "Rock": return "Rock";
                case "Salvaged Icepick": return "Salvaged Icepick";
                case "Satchel Charge": return "Satchel Charge";
                case "Semi - Automatic Pistol": return "Semi-Automatic Pistol";
                case "Stone Hatchet": return "Stone Hatchet";
                case "Stone Pick Axe": return "Stone Pickaxe";
                case "Sword": return "Salvaged Sword";
                case "Thompson": return "Thompson";
                case "Hammer": return "Hammer";
                case "Hatchet": return "Hatchet";
                case "Pick Axe": return "Pickaxe";
                case "Revolver": return "Revolver";
                case "Rocket Launcher": return "Rocket Launcher";
                case "Semi - Automatic Rifle": return "Semi-Automatic Rifle";
                case "Waterpipe Shotgun": return "Waterpipe Shotgun";
                case "Custom SMG": return "Custom SMG";
                case "Python": return "Python Revolver";
                case "LR300": return "LR300";
                case "Combat Knife": return "Combat Knife";
                case "Concrete Barricade": return "Concrete Barricade";
                case "Large Wood Box": return "Large Wood Box";
                case "Reactive Target": return "Reactive Target";
                case "Sandbag Barricade": return "Sandbag Barricade";
                case "Sleeping Bag": return "Sleeping Bag";
                case "Water Purifier": return "Water Purifier";
                case "Wood Storage Box": return "Wood Storage Box";
                case "Acoustic Guitar": return "Acoustic Guitar";
                case "Armored Door": return "Armored Door";
                case "Armored Double Door": return "Armored Double Door";
                case "Chair": return "Chair";
                case "Fridge": return "Fridge";
                case "Furnace": return "Furnace";
                case "Garage Door": return "Garage Door";
                case "Hunting Bow": return "Hunting Bow";
                case "Jackhammer": return "Jackhammer";
                case "L96": return "L96 Rifle";
                case "Locker": return "Locker";
                case "M249": return "M249";
                case "M39": return "M39 Rifle";
                case "Roadsign Gloves": return "Roadsign Gloves";
                case "Rug": return "Rug";
                case "Bearskin Rug": return "Rug Bear Skin";
                case "Sheet Metal Door": return "Sheet Metal Door";
                case "Sheet Metal Double Door": return "Sheet Metal Double Door";
                case "Table": return "Table";
                case "Vending Machine": return "Vending Machine";
                case "Wood Double Door": return "Wood Double Door";
                case "Wooden Door": return "Wooden Door";
                default: return workshopTag;
            }
        }

        public static string RustItemTypeToShortName(this string itemType)
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
                case "Mace": return "mace";
                case "Torch": return "torch";
                case "Building Skin": return "buildingskin";
                default: return "miscellanous";
            }
        }

        public static string RustItemShortNameToItemType(this string shortName)
        {
            switch (shortName.ToLower())
            {
                case "fun.guitar": return "Acoustic Guitar";
                case "door.hinged.toptier": return "Armored Door";
                case "door.double.hinged.toptier": return "Armored Double Door";
                case "rifle.ak": return "Assault Rifle";
                case "mask.bandana": return "Bandana Mask";
                case "hat.cap": return "Baseball Cap";
                case "hat.beenie": return "Beenie Hat";
                case "rifle.bolt": return "Bolt Action Rifle"; // texture issues (Tempered)
                case "bone.club": return "Bone Club";
                case "deer.skull.mask": return "Bone Helmet";
                case "knife.bone": return "Bone Knife";
                case "hat.boonie": return "Boonie Hat";
                case "shoes.boots": return "Boots";
                case "bucket.helmet": return "Bucket Helmet";
                case "burlap.headwrap": return "Burlap Headwrap";
                case "burlap.shirt": return "Burlap Shirt";
                case "burlap.shoes": return "Burlap Shoes";
                case "burlap.trousers": return "Burlap Trousers";
                case "chair": return "Chair";
                case "coffeecan.helmet": return "Coffee Can Helmet";
                case "knife.combat": return "Combat Knife";
                case "barricade.concrete": return "Concrete Barricade";
                case "crossbow": return "Crossbow"; // texture issues (Tempered)
                case "smg.2": return "Custom SMG"; // texture issues (Lovely SMG)
                case "shotgun.double": return "Double Barrel Shotgun"; // broken model
                case "pistol.eoka": return "Eoka Pistol";
                case "grenade.f1": return "F1 Grenade";
                case "fridge": return "Fridge"; // door is open
                case "furnace": return "Furnace";
                case "wall.frame.garagedoor": return "Garage Door";
                case "hammer": return "Hammer";
                case "hatchet": return "Hatchet";
                case "hazmatsuit": return "Hazmat Suit";
                case "attire.hide.boots": return "Hide Boots";
                case "attire.hide.helterneck": return "Hide Halterneck";
                case "attire.hide.pants": return "Hide Pants";
                case "attire.hide.poncho": return "Hide Poncho";
                case "attire.hide.skirt": return "Hide Skirt";
                case "attire.hide.vest": return "Hide Vest";
                case "hoodie": return "Hoodie";
                case "bow.hunting": return "Hunting Bow";
                case "mask.balaclava": return "Improvised Balaclava";
                case "jacket": return "Jacket";
                case "jackhammer": return "Jackhammer";
                case "rifle.l96": return "L96 Rifle";
                case "box.wooden.large": return "Large Wood Box"; // texture issues (Large Stickered Toy Car)
                case "burlap.gloves": return "Leather Gloves";
                case "locker": return "Locker"; // texture issues (Heli Cargo), doors are open
                case "tshirt.long": return "Longsleeve T-Shirt";
                case "longsword": return "Longsword";
                case "rifle.lr300": return "LR300";
                case "lmg.m249": return "M249"; // broken model
                case "rifle.m39": return "M39 Rifle";
                case "metal.plate.torso": return "Metal Chest Plate";
                case "metal.facemask": return "Metal Facemask";
                case "hat.miner": return "Miners Hat";
                case "smg.mp5": return "MP5A4";
                case "pants": return "Pants";
                case "pickaxe": return "Pickaxe";
                case "shotgun.pump": return "Pump Shotgun"; // can't hide bullet
                case "pistol.python": return "Python Revolver"; // can't hide chamber
                case "target.reactive": return "Reactive Target";
                case "pistol.revolver": return "Revolver";
                case "riot.helmet": return "Riot Helmet"; // broken model
                case "roadsign.jacket": return "Road Sign Jacket";
                case "roadsign.kilt": return "Road Sign Kilt";
                case "roadsign.gloves": return "Roadsign Gloves";
                case "rock": return "Rock";
                case "rocket.launcher": return "Rocket Launcher";
                case "rug": return "Rug";
                case "rug.bear": return "Rug Bear Skin";
                case "icepick.salvaged": return "Salvaged Icepick";
                case "salvaged.sword": return "Salvaged Sword";
                case "barricade.sandbags": return "Sandbag Barricade";
                case "explosive.satchel": return "Satchel Charge"; // broken model
                case "pistol.semiauto": return "Semi-Automatic Pistol";
                case "rifle.semiauto": return "Semi-Automatic Rifle";
                case "door.hinged.metal": return "Sheet Metal Door";
                case "door.double.hinged.metal": return "Sheet Metal Double Door";
                case "shirt.collared": return "Shirt"; // texture issues (torso should map to white)
                case "pants.shorts": return "Shorts";
                case "sleepingbag": return "Sleeping Bag";
                case "jacket.snow": return "Snow Jacket";
                case "stonehatchet": return "Stone Hatchet";
                case "stone.pickaxe": return "Stone Pickaxe";
                case "table": return "Table";
                case "shirt.tanktop": return "Tank Top";
                case "smg.thompson": return "Thompson";
                case "tshirt": return "T-Shirt";
                case "vending.machine": return "Vending Machine"; // broken model
                case "water.purifier": return "Water Purifier";
                case "shotgun.waterpipe": return "Waterpipe Shotgun";
                case "door.double.hinged.wood": return "Wood Double Door";
                case "box.wooden": return "Wood Storage Box"; // texture issues (Heli Cargo)
                case "door.hinged.wood": return "Wooden Door";
                case "mace": return "Mace";
                case "torch": return "Torch";
                case "buildingskin": return "Building Skin";
                default: return "Miscellanous";
            }
        }
        public static string RustItemTypeGroup(this string itemType)
        {
            if (IsRustArmourItemType(itemType))
            {
                return "Armour";
            }
            else if (IsRustClothingItemType(itemType))
            {
                return "Clothing";
            }
            else if (IsRustGunItemType(itemType))
            {
                return "Guns";
            }
            else if (IsRustWeaponItemType(itemType))
            {
                return "Weapons";
            }
            else if (IsRustToolItemType(itemType))
            {
                return "Tools";
            }
            else if (IsRustConstructionItemType(itemType))
            {
                return "Construction";
            }
            else if (IsRustDeployableItemType(itemType))
            {
                return "Deployables";
            }
            else if (IsRustFunItemType(itemType))
            {
                return "Fun";
            }

            return null;
        }

        public static bool IsRustArmourItemType(this string itemType)
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

        public static bool IsRustClothingItemType(this string itemType)
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

        public static bool IsRustGunItemType(this string itemType)
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

        public static bool IsRustWeaponItemType(this string itemType)
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
                case "Bone Club":
                case "Mace": return true;
                default: return false;
            }
        }

        public static bool IsRustToolItemType(this string itemType)
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
                case "Rock":
                case "Torch": return true;
                default: return false;
            }
        }

        public static bool IsRustConstructionItemType(this string itemType)
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

        public static bool IsRustDeployableItemType(this string itemType)
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

        public static bool IsRustFunItemType(this string itemType)
        {
            switch (itemType)
            {
                case "Acoustic Guitar": return true;
                default: return false;
            }
        }

        public static bool IsRustSpecialItemType(this string itemType)
        {
            return (
                !itemType.IsRustArmourItemType() &&
                !itemType.IsRustClothingItemType() &&
                !itemType.IsRustGunItemType() &&
                !itemType.IsRustWeaponItemType() &&
                !itemType.IsRustToolItemType() &&
                !itemType.IsRustConstructionItemType() &&
                !itemType.IsRustDeployableItemType() &&
                !itemType.IsRustFunItemType()
            );
        }
    }
}
