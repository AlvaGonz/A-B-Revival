package f4data.sdata
{
   import f4.data.InitData;
   import f4data.F4DataHole;
   import f4data.F4DataLinks;
   import f4data.F4DataOrg;
   import f4data.cpx.F4DataCPX;
   import flash.display.Stage;
   import flash.display.StageScaleMode;
   import flash.display.StageAlign;
   import flash.display.StageQuality;
   import flash.display.StageDisplayState;
   import flash.events.Event;
   import flash.net.SharedObject;
   import flash.ui.Mouse;
   import flash.utils.ByteArray;
   import flash.utils.getTimer;
   import fullshit.AbsMain;
   import f4data.CompanionAssignment;
   
   public class ConfigAndStory
   {
      
      public static const RES_640:int = 0;
      
      public static const RES_720:int = 1;
      
      public static const RES_1080:int = 2;
      
      public static const QUALITY_LOW:int = 0;
      
      public static const QUALITY_MEDIUM:int = 1;
      
      public static const QUALITY_HIGH:int = 2;
      
      public static var RESOLUTION:int = RES_1080;
      
      public static var QUALITY:int = QUALITY_MEDIUM;
      
      public static var FULLSCREEN:Boolean = false;
      
      public static var CONFIG:Array;
      
      public static var TCONFIG:Array;
      
      public static var STORY:int;
      
      public static var S100LOCK:Boolean;
      
      public static var S100LOCKINT:int;
      
      public static var PLAYER:int;
      
      public static var COMPANIONS:Array;
      
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
         CONFIG = [1,0,1,1,1,1,0];
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
         COMPANIONS = [];
         AIGAME = true;
         DIRECTOR = new Director();
         MISSIONS = new MissionData();
         initGift();
         load();
         loadData();
      }
      
      public static function applyResolution(param1:int) : void
      {
         RESOLUTION = param1;
         var _targetW:int = 640;
         var _targetH:int = 480;
         if(param1 == RES_720)
         {
            _targetW = 1280;
            _targetH = 720;
         }
         else if(param1 == RES_1080)
         {
            _targetW = 1920;
            _targetH = 1080;
         }
         var _stg:Stage = AbsMain.STAGE;
         if(_stg)
         {
            if(param1 == RES_720 || param1 == RES_1080 || FULLSCREEN)
            {
               _stg.scaleMode = StageScaleMode.EXACT_FIT;
            }
            else
            {
               _stg.scaleMode = StageScaleMode.SHOW_ALL;
            }
            _stg.align = StageAlign.TOP;
            try
            {
               if("nativeWindow" in _stg && _stg["nativeWindow"] != null)
               {
                  _stg["nativeWindow"]["width"] = _targetW;
                  _stg["nativeWindow"]["height"] = _targetH;
               }
            }
            catch(e:Error)
            {
            }
         }
         if(InitData.RENDERDATA)
         {
            InitData.RENDERDATA.W = 640;
            InitData.RENDERDATA.H = 480;
         }
      }

      public static function applyQuality(param1:int) : void
      {
         QUALITY = param1;
         var _stg:Stage = AbsMain.STAGE;
         if(_stg)
         {
            if(param1 == QUALITY_LOW)
            {
               _stg.quality = StageQuality.LOW;
               CONFIG[0] = 0;
               CONFIG[1] = 0;
            }
            else if(param1 == QUALITY_MEDIUM)
            {
               _stg.quality = StageQuality.MEDIUM;
               CONFIG[0] = 1;
               CONFIG[1] = 0;
            }
            else
            {
               _stg.quality = StageQuality.HIGH;
               CONFIG[0] = 1;
               CONFIG[1] = 1;
            }
         }
         if(InitData.RENDERDATA)
         {
            InitData.RENDERDATA.sms = (param1 != QUALITY_LOW);
         }
      }

      public static function setFullscreen(param1:Boolean) : Boolean
      {
         var _stg:Stage = AbsMain.STAGE;
         if(!_stg)
         {
            return false;
         }
         try
         {
            if(param1)
            {
               _stg.displayState = StageDisplayState.FULL_SCREEN_INTERACTIVE;
               _stg.scaleMode = StageScaleMode.EXACT_FIT;
               FULLSCREEN = true;
            }
            else
            {
               _stg.displayState = StageDisplayState.NORMAL;
               if(RESOLUTION == RES_640)
               {
                  _stg.scaleMode = StageScaleMode.SHOW_ALL;
               }
               else
               {
                  _stg.scaleMode = StageScaleMode.EXACT_FIT;
               }
               FULLSCREEN = false;
            }
            return true;
         }
         catch(e:Error)
         {
            try
            {
               if(param1)
               {
                  _stg.displayState = StageDisplayState.FULL_SCREEN;
                  _stg.scaleMode = StageScaleMode.EXACT_FIT;
                  FULLSCREEN = true;
               }
               else
               {
                  _stg.displayState = StageDisplayState.NORMAL;
                  if(RESOLUTION == RES_640)
                  {
                     _stg.scaleMode = StageScaleMode.SHOW_ALL;
                  }
                  else
                  {
                     _stg.scaleMode = StageScaleMode.EXACT_FIT;
                  }
                  FULLSCREEN = false;
               }
               return true;
            }
            catch(e2:Error)
            {
            }
         }
         return false;
      }

      public static function toggleFullscreen() : Boolean
      {
         return setFullscreen(!FULLSCREEN);
      }

      public static function makeChange() : void
      {
         applyQuality(QUALITY);
         applyResolution(RESOLUTION);
         if(FULLSCREEN)
         {
            setFullscreen(true);
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
         if(InitData.RENDERDATA)
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
            if(_loc1_ && _loc1_.isCaptainShip())
            {
               return _loc1_;
            }
         }
         return null;
      }
      
      private static function cloneFlyTree(param1:F4DataLinks) : F4DataLinks
      {
         var _loc2_:F4DataOrg = null;
         var _loc3_:F4DataLinks = null;
         var _loc4_:int = 0;
         var _loc5_:int = 0;
         var _loc6_:F4DataHole = null;
         var _loc7_:F4DataHole = null;
         var _loc8_:F4DataLinks = null;
         if(!param1 || !param1.f4dataorg)
         {
            return null;
         }
         _loc2_ = param1.f4dataorg.clone(ALLPART.defFire,ALLPART.defBullet);
         _loc2_.index = ALLPART.plPart.length;
         ALLPART.plPart.push(_loc2_);
         _loc3_ = new F4DataLinks();
         _loc3_.f4dataorg = _loc2_;
         _loc3_.name = param1.name;
         _loc3_.colorHue = param1.colorHue;
         _loc3_.special = param1.special;
         if(Boolean(param1.f4dataorg.hole) && Boolean(_loc2_.hole))
         {
            _loc4_ = int(param1.f4dataorg.hole.length);
            _loc5_ = 0;
            while(_loc5_ < _loc4_)
            {
               _loc6_ = param1.f4dataorg.hole[_loc5_] as F4DataHole;
               _loc7_ = _loc2_.hole[_loc5_] as F4DataHole;
               if(Boolean(_loc6_) && Boolean(_loc6_.partf4) && Boolean(_loc7_))
               {
                  _loc8_ = cloneFlyTree(_loc6_.partf4);
                  if(_loc8_)
                  {
                     _loc7_.partf4 = _loc8_;
                     _loc8_.atHole = _loc7_;
                     _loc8_.parentPart = _loc3_;
                  }
               }
               _loc5_++;
            }
         }
         return _loc3_;
      }
      
      public static function checkCaptainShip() : void
      {
         var _loc1_:F4DataLinks = null;
         var _loc2_:F4DataLinks = null;
         var _loc3_:int = 0;
         var _loc4_:F4DataLinks = null;
         if(STORY >= 3 && Boolean(ALLPART) && Boolean(ALLPART.plFly) && Boolean(ALLPART.defFly) && Boolean(ALLPART.defFly[76]))
         {
            if(getCaptainFly() != null)
            {
               return;
            }
            _loc1_ = ALLPART.defFly[76] as F4DataLinks;
            if(_loc1_)
            {
               _loc2_ = cloneFlyTree(_loc1_);
               if(_loc2_)
               {
                  _loc2_.name = "Captain's Ship";
                  _loc2_.special = true;
                  _loc3_ = 0;
                  for each(_loc4_ in ALLPART.plFly)
                  {
                     if(_loc4_ && _loc4_.index >= _loc3_)
                     {
                        _loc3_ = _loc4_.index + 1;
                     }
                  }
                  _loc2_.index = _loc3_;
                  ALLPART.plFly[_loc3_] = _loc2_;
                  saveData();
               }
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
            if(COMPANIONS && COMPANIONS[_loc1_] != null)
            {
               var _cRecord:CompanionAssignment = COMPANIONS[_loc1_] as CompanionAssignment;
               if(_cRecord && (_cRecord.craftIndex == PLAYER || !ALLPART.plFly[_cRecord.craftIndex]))
               {
                  COMPANIONS[_loc1_] = null;
               }
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
            if(CONFIG.length < 7 || CONFIG[6] == null)
            {
               CONFIG[6] = 0;
            }
            TIMEALL = SO.data["time"];
            if(SO.data["autosave"] != null)
            {
               AUTOSAVE = Boolean(SO.data["autosave"]);
            }
            if(SO.data["resolution"] != null)
            {
               RESOLUTION = int(SO.data["resolution"]);
            }
            if(SO.data["quality"] != null)
            {
               QUALITY = int(SO.data["quality"]);
            }
            if(SO.data["fullscreen"] != null)
            {
               FULLSCREEN = Boolean(SO.data["fullscreen"]);
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
         SO.data["resolution"] = RESOLUTION;
         SO.data["quality"] = QUALITY;
         SO.data["fullscreen"] = FULLSCREEN;
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
            AIGAME = SOData.data["aigame"];
            HOWTOPLAY = SOData.data["how"];
            SHOWTEXTMENU = SOData.data["menu"];
            COMPANIONS = [];
            var _rawComps:Array = SOData.data["companions"] as Array;
            if(_rawComps != null)
            {
               var _ri:int = 0;
               while(_ri < _rawComps.length)
               {
                  if(_rawComps[_ri] != null)
                  {
                     COMPANIONS[_ri] = CompanionAssignment.fromObject(_rawComps[_ri]);
                  }
                  else
                  {
                     COMPANIONS[_ri] = null;
                  }
                  _ri++;
               }
            }
            else if(SOData.data["ai"] != null)
            {
               var _legacyAI:Array = SOData.data["ai"] as Array;
               var _li:int = 0;
               while(_li < _legacyAI.length)
               {
                  if(_legacyAI[_li] != null && int(_legacyAI[_li]) >= 0)
                  {
                     COMPANIONS[_li] = new CompanionAssignment(int(_legacyAI[_li]), CompanionAssignment.ROLE_ESCORT, CompanionAssignment.LEADER_MICHAEL);
                  }
                  else
                  {
                     COMPANIONS[_li] = null;
                  }
                  _li++;
               }
            }
            checkCaptainShip();
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
         SOData.data["aigame"] = AIGAME;
         var _compList:Array = [];
         if(COMPANIONS)
         {
            var _ci:int = 0;
            while(_ci < COMPANIONS.length)
            {
               if(COMPANIONS[_ci] is CompanionAssignment)
               {
                  _compList.push(CompanionAssignment(COMPANIONS[_ci]).toObject());
               }
               else if(COMPANIONS[_ci] != null)
               {
                  _compList.push(CompanionAssignment.fromObject(COMPANIONS[_ci]).toObject());
               }
               else
               {
                  _compList.push(null);
               }
               _ci++;
            }
         }
         SOData.data["companions"] = _compList;
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
         _loc1_["aigame"] = AIGAME;
         var _outComps:Array = [];
         if(COMPANIONS)
         {
            var _oi:int = 0;
            while(_oi < COMPANIONS.length)
            {
               if(COMPANIONS[_oi] is CompanionAssignment)
               {
                  _outComps.push(CompanionAssignment(COMPANIONS[_oi]).toObject());
               }
               else if(COMPANIONS[_oi] != null)
               {
                  _outComps.push(CompanionAssignment.fromObject(COMPANIONS[_oi]).toObject());
               }
               else
               {
                  _outComps.push(null);
               }
               _oi++;
            }
         }
         _loc1_["companions"] = _outComps;
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
               AIGAME = obj["aigame"];
               HOWTOPLAY = obj["how"];
               SHOWTEXTMENU = obj["menu"];
               COMPANIONS = [];
               var _objComps:Array = obj["companions"] as Array;
               if(_objComps != null)
               {
                  var _oji:int = 0;
                  while(_oji < _objComps.length)
                  {
                     if(_objComps[_oji] != null)
                     {
                        COMPANIONS[_oji] = CompanionAssignment.fromObject(_objComps[_oji]);
                     }
                     else
                     {
                        COMPANIONS[_oji] = null;
                     }
                     _oji++;
                  }
               }
               else if(obj["ai"] != null)
               {
                  var _legacyObjAI:Array = obj["ai"] as Array;
                  var _loi:int = 0;
                  while(_loi < _legacyObjAI.length)
                  {
                     if(_legacyObjAI[_loi] != null && int(_legacyObjAI[_loi]) >= 0)
                     {
                        COMPANIONS[_loi] = new CompanionAssignment(int(_legacyObjAI[_loi]), CompanionAssignment.ROLE_ESCORT, CompanionAssignment.LEADER_MICHAEL);
                     }
                     else
                     {
                        COMPANIONS[_loi] = null;
                     }
                     _loi++;
                  }
               }
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
         COMPANIONS = [];
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
            GIFT[_loc6_] = [_loc1_,_loc1_ + 1,_loc2_,_loc3_,_loc4_,_loc5_];
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

