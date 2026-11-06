package f4data.sdata
{
   import f4.data.InitData;
   import f4data.F4DataLinks;
   import f4data.cpx.F4DataCPX;
   import flash.events.Event;
   import flash.net.SharedObject;
   import flash.ui.Mouse;
   import flash.utils.ByteArray;
   import flash.utils.getTimer;
   
   public class ConfigAndStory
   {
      
      public static var CONFIG:Array;
      
      public static var TCONFIG:Array;
      
      public static var STORY:int;
      
      public static var S100LOCK:Boolean;
      
      public static var S100LOCKINT:int;
      
      public static var PLAYER:int;
      
      public static var AI:Array;
      
      public static var MONEY:int;
      
      public static var AIGAME:Boolean;
      
      public static var ALLPART:F4DataCPX;
      
      public static var DIRECTOR:Director;
      
      public static var MISSIONS:MissionData;
      
      public static var CKTIME:uint;
      
      public static var TIMEALL:uint;
      
      public static var HOWTOPLAY:Array;
      
      public static var SHOWTEXTMENU:Array;
      
      public static var SHOPNEW:Boolean;
      
      public static var SO:SharedObject;
      
      public static var SOData:SharedObject;
      
      public static var AUTOSAVE:Boolean = true;
      
      public static const GIFT:Array = [];
      
      public static const MAP1:int = 35;
      
      public function ConfigAndStory()
      {
         super();
      }
      
      public static function init() : void
      {
         CONFIG = [1,0,1,1,1,1];
         SHOWTEXTMENU = [0,0,0,0,0];
         HOWTOPLAY = [0,0,0,0,0,0,0,0,0];
         STORY = 0;
         if(VeryOutShitABC.VGPLAY)
         {
            MONEY = 100000;
         }
         else
         {
            MONEY = 0;
         }
         PLAYER = 0;
         TIMEALL = 0;
         AI = [];
         AIGAME = true;
         DIRECTOR = new Director();
         MISSIONS = new MissionData();
         initGift();
         load();
         loadData();
      }
      
      public static function makeChange() : void
      {
         if(CONFIG[0] == 0 && CONFIG[1] == 0)
         {
            InitData.RENDERDATA.sms = false;
         }
         else
         {
            InitData.RENDERDATA.sms = true;
         }
         if(CONFIG[4] == 0)
         {
            InitData.SOUNDC2.allEnd(false);
            InitData.SOUNDC2.nowVol = 0.01;
         }
         else
         {
            InitData.SOUNDC2.nowVol = 1;
            InitData.SOUNDC2.allResume();
         }
         if(CONFIG[2] == 0)
         {
            InitData.RENDERDATA.W = 512;
            InitData.RENDERDATA.H = 384;
            InitData.RENDERDATA.init();
         }
         else
         {
            InitData.RENDERDATA.W = 640;
            InitData.RENDERDATA.H = 480;
            InitData.RENDERDATA.init();
         }
         save();
      }
      
      public static function nowChoose() : Boolean
      {
         if(STORY == 0 || STORY == 8 || STORY == 9 || STORY == 49)
         {
            return false;
         }
         return true;
      }
      
      public static function noNext() : Boolean
      {
         if(STORY == 0 || STORY == 7 || STORY == 9 || STORY == 49)
         {
            return false;
         }
         return true;
      }
      
      public static function getCaptainFly() : F4DataLinks
      {
         var _loc1_:F4DataLinks = null;
         if(!ALLPART || !ALLPART.plFly)
         {
            return null;
         }
         for each(_loc1_ in ALLPART.plFly)
         {
            if(_loc1_ && (_loc1_.special || _loc1_.name == "Captain's Ship" || _loc1_.name.indexOf("Captain") != -1))
            {
               return _loc1_;
            }
         }
         return null;
      }
      
      public static function checkCaptainShip() : void
      {
         var _loc1_:F4DataLinks = null;
         var _loc2_:F4DataLinks = null;
         if(STORY >= 3 && Boolean(ALLPART) && Boolean(ALLPART.plFly) && Boolean(ALLPART.defFly) && Boolean(ALLPART.defFly[76]))
         {
            if(getCaptainFly() != null)
            {
               return;
            }
            _loc1_ = ALLPART.defFly[76] as F4DataLinks;
            if(_loc1_)
            {
               _loc2_ = _loc1_.clone(ALLPART.plPart,ALLPART.defFire,ALLPART.defBullet);
               _loc2_.name = "Captain's Ship";
               _loc2_.special = true;
               _loc2_.index = ALLPART.plFly.length;
               ALLPART.plFly.push(_loc2_);
            }
         }
      }
      
      public static function ckAI() : void
      {
         var _loc2_:F4DataLinks = null;
         if(!ALLPART.plFly[PLAYER])
         {
            var _loc3_:int = 0;
            var _loc4_:* = ALLPART.plFly;
            for each(_loc2_ in _loc4_)
            {
               PLAYER = _loc2_.index;
            }
         }
         var _loc1_:int = 0;
         while(_loc1_ < 20)
         {
            if(AI[_loc1_] == PLAYER || !ALLPART.plFly[AI[_loc1_]])
            {
               AI[_loc1_] = null;
            }
            _loc1_++;
         }
      }
      
      public static function load() : void
      {
         SO = SharedObject.getLocal("fly4");
         if(SO.data["config"] != null)
         {
            CONFIG = SO.data["config"];
            TIMEALL = SO.data["time"];
            if(SO.data["autosave"] != null)
            {
               AUTOSAVE = Boolean(SO.data["autosave"]);
            }
         }
         else
         {
            save();
         }
      }
      
      public static function save() : void
      {
         TIMEALL += (getTimer() - CKTIME) / 1000;
         CKTIME = getTimer();
         SO.data["config"] = CONFIG.concat();
         SO.data["time"] = TIMEALL;
         SO.data["autosave"] = AUTOSAVE;
         SO.flush();
      }
      
      public static function autoSaveMission() : Boolean
      {
         if(!AUTOSAVE)
         {
            return false;
         }
         saveData();
         save();
         return true;
      }
      
      public static function loadData() : void
      {
         SOData = SharedObject.getLocal("fly4data");
         if(SOData.data["data"] != null)
         {
            ALLPART.loadSO(SOData.data["data"]);
            STORY = SOData.data["story"];
            MONEY = SOData.data["money"];
            PLAYER = SOData.data["player"];
            AI = SOData.data["ai"];
            AIGAME = SOData.data["aigame"];
            HOWTOPLAY = SOData.data["how"];
            SHOWTEXTMENU = SOData.data["menu"];
         }
         else
         {
            saveData();
         }
      }
      
      public static function saveData() : void
      {
         SOData.data["data"] = ALLPART.saveSO();
         if(STORY == 49)
         {
            SOData.data["story"] = 99;
         }
         else
         {
            SOData.data["story"] = STORY;
         }
         SOData.data["money"] = MONEY;
         SOData.data["player"] = PLAYER;
         SOData.data["ai"] = AI.concat();
         SOData.data["aigame"] = AIGAME;
         SOData.data["how"] = HOWTOPLAY.concat();
         SOData.data["menu"] = SHOWTEXTMENU.concat();
         SOData.flush();
      }
      
      public static function makeOutData() : ByteArray
      {
         var _loc1_:Object = new Object();
         _loc1_["title"] = "fly4Save";
         _loc1_["data"] = ALLPART.saveSO();
         _loc1_["story"] = STORY;
         _loc1_["money"] = MONEY;
         _loc1_["player"] = PLAYER;
         _loc1_["ai"] = AI.concat();
         _loc1_["aigame"] = AIGAME;
         _loc1_["how"] = HOWTOPLAY.concat();
         _loc1_["menu"] = SHOWTEXTMENU.concat();
         var _loc2_:ByteArray = new ByteArray();
         _loc2_.writeObject(_loc1_);
         _loc2_.position = 0;
         _loc2_.compress();
         _loc2_.position = 0;
         return _loc2_;
      }
      
      public static function readOutData(param1:ByteArray) : Boolean
      {
         var tba:ByteArray = null;
         var obj:Object = null;
         var ba:ByteArray = param1;
         try
         {
            tba = new ByteArray();
            tba.writeBytes(ba);
            tba.position = 0;
            tba.uncompress();
            tba.position = 0;
            obj = tba.readObject();
            if(Boolean(obj["title"]) && obj["title"] == "fly4Save")
            {
               ALLPART.loadSO(obj["data"]);
               STORY = obj["story"];
               MONEY = obj["money"];
               PLAYER = obj["player"];
               AI = obj["ai"];
               AIGAME = obj["aigame"];
               HOWTOPLAY = obj["how"];
               SHOWTEXTMENU = obj["menu"];
               saveData();
               InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[17]);
               return true;
            }
         }
         catch(e:Error)
         {
            trace(e);
            InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[18]);
            return false;
         }
         InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[18]);
         return false;
      }
      
      public static function cleanData() : void
      {
         SOData.clear();
         ALLPART.shopPart.length = 0;
         ALLPART.plPart.length = 0;
         ALLPART.plFly.length = 0;
         Fly4UI.addRandomData();
         STORY = 0;
         if(VeryOutShitABC.VGPLAY)
         {
            MONEY = 100000;
         }
         else
         {
            MONEY = 0;
         }
         PLAYER = 0;
         AI = [];
         AIGAME = true;
         HOWTOPLAY = [0,0,0,0,0,0,0,0,0];
         SHOWTEXTMENU = [0,0,0,0,0];
      }
      
      public static function initGift() : void
      {
         var _loc1_:int = 0;
         var _loc2_:int = 0;
         var _loc3_:int = 0;
         var _loc4_:int = 0;
         var _loc5_:int = 0;
         var _loc8_:int = 0;
         _loc1_ = 0;
         _loc2_ = 71;
         _loc3_ = 133;
         _loc4_ = 173;
         _loc5_ = 222;
         var _loc6_:int = 0;
         while(_loc6_ < 35)
         {
            GIFT[_loc6_] = [_loc1_,_loc1_ + 1,_loc2_,_loc2_,_loc2_,_loc2_,_loc3_,_loc3_,_loc3_,_loc3_,_loc4_,_loc5_];
            _loc1_ += 2;
            _loc3_++;
            _loc4_++;
            _loc5_++;
            if(_loc6_ % 2 == 0)
            {
               _loc2_ += 1;
            }
            else
            {
               (GIFT[_loc6_] as Array).push(_loc2_ + 1);
               (GIFT[_loc6_] as Array).push(_loc2_ + 1);
               (GIFT[_loc6_] as Array).push(_loc2_ + 1);
               (GIFT[_loc6_] as Array).push(_loc2_ + 1);
               _loc2_ += 2;
            }
            _loc6_++;
         }
         var _loc7_:Array = GIFT[34];
         _loc7_.push(70);
         _loc8_ = 133 + 35 - 1;
         while(_loc8_ < 173)
         {
            _loc7_.push(_loc8_);
            _loc7_.push(_loc8_);
            _loc7_.push(_loc8_);
            _loc7_.push(_loc8_);
            _loc8_++;
         }
         _loc8_ = 173 + 35 - 1;
         while(_loc8_ < 222)
         {
            _loc7_.push(_loc8_);
            _loc8_++;
         }
         _loc8_ = 222 + 35 - 1;
         while(_loc8_ < 271)
         {
            _loc7_.push(_loc8_);
            _loc8_++;
         }
         _loc8_ = 71 + 53 - 1;
         while(_loc8_ < 133)
         {
            _loc7_.push(_loc8_);
            _loc7_.push(_loc8_);
            _loc7_.push(_loc8_);
            _loc7_.push(_loc8_);
            _loc8_++;
         }
      }
      
      public static function num2LR(param1:int) : String
      {
         if(param1 == 0)
         {
            return "L";
         }
         if(param1 == 1)
         {
            return "R";
         }
         return "LR";
      }
      
      public static function num2Type(param1:int) : String
      {
         if(param1 == 0)
         {
            return "Body";
         }
         if(param1 == 1)
         {
            return "Wing";
         }
         if(param1 == 2)
         {
            return "Eng.";
         }
         if(param1 == 3)
         {
            return "Gun";
         }
         return "Other";
      }
      
      public static function addMouseLeave() : void
      {
         InitData.STAGE.addEventListener(Event.MOUSE_LEAVE,mouseLeave);
      }
      
      public static function removeMouseLeave() : void
      {
         InitData.STAGE.removeEventListener(Event.MOUSE_LEAVE,mouseLeave);
      }
      
      private static function mouseLeave(param1:Event) : void
      {
         Mouse.hide();
      }
   }
}

