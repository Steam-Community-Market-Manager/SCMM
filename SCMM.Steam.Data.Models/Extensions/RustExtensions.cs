namespace SCMM.Steam.Data.Models.Extensions
{
    public static class RustExtensions
    {
        public static string RustItemTypeToItemId(this string itemType)
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
                case "Double Barrel Shotgun": return "shotgun.double"; // model broken
                case "Eoka Pistol": return "pistol.eoka";
                case "F1 Grenade": return "grenade.f1";
                case "Fridge": return "fridge"; // door is open
                case "Furnace": return "furnace";
                case "Garage Door": return "wall.frame.garagedoor";
                case "Hammer": return "hammer";
                case "Hatchet": return "hatchet";
                case "Hide Boots": return "attire.hide.boots"; //...
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
                case "Large Wood Box": return "box.wooden.large";
                case "Leather Gloves": return "burlap.gloves"; // dafuq???
                case "Locker": return "locker";
                case "Longsleeve T-Shirt": return "tshirt.long";
                case "Longsword": return "longsword";
                case "LR300": return "lr300.item";
                case "M249": return "lmg.m249";
                case "M39 Rifle": return "rifle.m39";
                case "Metal Chest Plate": return "metal.plate.torso";
                case "Metal Facemask": return "metal.facemask";
                case "Miners Hat": return "hat.miner";
                case "MP5A4": return "smg.mp5";
                case "Pants": return "pants";
                case "Pickaxe": return "pickaxe";
                case "Pump Shotgun": return "shotgun.pump";
                case "Python Revolver": return "pistol.python";
                case "Reactive Target": return "target.reactive";
                case "Revolver": return "pistol.revolver";
                case "Riot Helmet": return "riot.helmet";
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
                case "Satchel Charge": return "explosive.satchel";
                case "Semi-Automatic Pistol": return "pistol.semiauto";
                case "Semi-Automatic Rifle": return "rifle.semiauto";
                case "Sheet Metal Door": return "door.hinged.metal";
                case "Sheet Metal Double Door": return "door.double.hinged.metal";
                case "Shirt": return "shirt.collared";
                case "Shorts": return "pants.shorts";
                case "Sleeping Bag": return "sleepingbag";
                case "Snow Jacket": return "jacket.snow";
                case "Stone Hatchet": return "stonehatchet";
                case "Stone Pickaxe": return "stone.pickaxe";
                case "Table": return "table";
                case "Tank Top": return "shirt.tanktop";
                case "Thompson": return "smg.thompson";
                case "T-Shirt": return "tshirt";
                case "Vending Machine": return "vending.machine";
                case "Water Purifier": return "water.purifier";
                case "Waterpipe Shotgun": return "shotgun.waterpipe";
                case "Wood Double Door": return "door.double.hinged.wood";
                case "Wood Storage Box": return "box.wooden";
                case "Wooden Door": return "door.hinged.wood";
                default: return null;
            }
        }
    }
}
