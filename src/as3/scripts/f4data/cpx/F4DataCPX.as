package f4data.cpx
{
   import all.AllData;
   import f4data.F4DataBullet;
   import f4data.F4DataFire;
   import f4data.F4DataHole;
   import f4data.F4DataLinks;
   import f4data.F4DataOrg;
   import f4data.MC2F4Data;
   import f4data.movie.MovieIndex;
   import f4data.sdata.ConfigAndStory;
   import f4data.talk.TalkIndex;
   import flash.display.MovieClip;
   import flash.geom.Point;
   import flash.utils.ByteArray;
   import fullshit.tool.Attach;
   
   public class F4DataCPX
   {
      
      public var defBullet:Array;
      
      public var defFire:Array;
      
      public var defPart:Array;
      
      public var defFly:Array;
      
      public var plPart:Array;
      
      public var plFly:Array;
      
      public var plMc:Array;
      
      public var shopPart:Array;
      
      public var repairPart:Array;
      
      public var itemPart:Array;
      
      public var levelMoney:int;
      
      public var talk:TalkIndex;
      
      public var movie:MovieIndex;
      
      public var alldata:AllData;
      
      public function F4DataCPX(param1:AllData)
      {
         super();
         this.alldata = param1;
         this.defBullet = [];
         this.defFire = [];
         this.defPart = [];
         this.defFly = [];
         this.itemPart = [];
         this.plPart = [];
         this.plFly = [];
         this.plMc = [];
         this.shopPart = [];
         this.repairPart = [];
         this.talk = new TalkIndex();
         this.movie = new MovieIndex();
         this.bulletCop();
         this.fireCop();
         this.partCop();
         this.talkCop();
         this.movCop();
         this.allLoad();
      }
      
      public function emptyPlPart() : Boolean
      {
         var _loc1_:F4DataOrg = null;
         var _loc2_:int = 0;
         var _loc3_:* = this.plPart;
         for each(_loc1_ in _loc3_)
         {
            return false;
         }
         return true;
      }
      
      public function reset() : void
      {
         this.defBullet.length = 0;
         this.defFire.length = 0;
         this.defPart.length = 0;
         this.defFly.length = 0;
         this.plPart.length = 0;
         this.plFly.length = 0;
         this.shopPart.length = 0;
         this.plMc.length = 0;
         this.repairPart.length = 0;
         this.itemPart.length = 0;
      }
      
      public function talkCop() : void
      {
         var _loc2_:XML = null;
         var _loc3_:Object = null;
         var _loc1_:XML = new XML(new this.alldata.talk());
         for each(_loc2_ in _loc1_["talkOne"])
         {
            _loc3_ = this.talk.xmlToObj(_loc2_);
            this.talk.talkData[_loc3_["index"]] = _loc3_;
         }
      }
      
      public function movCop() : void
      {
         var _loc2_:XML = null;
         var _loc3_:Object = null;
         var _loc1_:XML = new XML(new this.alldata.movie());
         for each(_loc2_ in _loc1_["movieOne"])
         {
            _loc3_ = this.xmlToObj(_loc2_);
            this.movie.movData[_loc3_["index"]] = _loc3_;
         }
      }
      
      public function xmlToObj(param1:XML) : Object
      {
         var _loc3_:XML = null;
         var _loc4_:String = null;
         var _loc5_:Number = NaN;
         var _loc2_:Object = new Object();
         for each(_loc3_ in param1.children())
         {
            _loc4_ = _loc3_.toString();
            _loc5_ = parseFloat(_loc4_);
            if(!isNaN(_loc5_))
            {
               _loc2_[_loc3_.name()] = _loc5_;
            }
            else
            {
               _loc2_[_loc3_.name()] = _loc4_;
            }
         }
         return _loc2_;
      }
      
      public function bulletCop() : void
      {
         var _loc2_:XML = null;
         var _loc3_:Object = null;
         var _loc4_:F4DataBullet = null;
         var _loc1_:XML = new XML(new this.alldata.defBullet());
         for each(_loc2_ in _loc1_["bulletOne"])
         {
            _loc3_ = this.xmlToObj(_loc2_);
            _loc4_ = new F4DataBullet();
            _loc4_.readFromObj(_loc3_,true);
            this.defBullet[_loc4_.index] = _loc4_;
         }
      }
      
      public function fireCop() : void
      {
         var _loc2_:XML = null;
         var _loc3_:Object = null;
         var _loc4_:F4DataFire = null;
         var _loc1_:XML = new XML(new this.alldata.defFire());
         for each(_loc2_ in _loc1_["fireOne"])
         {
            _loc3_ = this.xmlToObj(_loc2_);
            _loc4_ = new F4DataFire();
            _loc4_.readFromObj(_loc3_);
            this.defFire[_loc4_.index] = _loc4_;
         }
      }
      
      public function partCop() : void
      {
         var _loc2_:XML = null;
         var _loc3_:Object = null;
         var _loc4_:F4DataOrg = null;
         var _loc1_:XML = new XML(new this.alldata.defPart());
         for each(_loc2_ in _loc1_["partOne"])
         {
            _loc3_ = this.xmlToObj(_loc2_);
            _loc4_ = new F4DataOrg();
            _loc4_.readFromObj(_loc3_,this.defFire,this.defBullet,true);
            this.defPart[_loc4_.index] = _loc4_;
         }
      }
      
      public function allLoad() : void
      {
         this.updateBltByMc();
         this.updateOrgByMc();
         this.readDefFlyNow();
      }
      
      public function updateOrgByMc() : void
      {
         var _loc1_:MovieClip = null;
         var _loc3_:F4DataOrg = null;
         var _loc2_:MC2F4Data = new MC2F4Data();
         for each(_loc3_ in this.defPart)
         {
            _loc1_ = Attach.attach(_loc3_.mcName) as MovieClip;
            _loc2_.readMcData(_loc1_,_loc3_);
         }
      }
      
      public function updateBltByMc() : void
      {
         var _loc1_:MovieClip = null;
         var _loc3_:F4DataBullet = null;
         var _loc2_:MC2F4Data = new MC2F4Data();
         for each(_loc3_ in this.defBullet)
         {
            if(_loc3_.mcName != "normal")
            {
               _loc1_ = Attach.attach(_loc3_.mcName) as MovieClip;
               _loc2_.readMcDataBlt(_loc1_,_loc3_);
            }
            else if(_loc3_.btwh > 0)
            {
               _loc3_.hit = new Point(_loc3_.btwh,_loc3_.btwh);
            }
            else
            {
               _loc3_.hit = new Point(2,2);
            }
         }
      }
      
      public function addPartFromShop(param1:int) : void
      {
         var _shopItem:F4DataOrg = this.shopPart[param1];
         if(!_shopItem)
         {
            return;
         }
         var _loc2_:F4DataOrg = _shopItem.clone(this.defFire,this.defBullet);
         _loc2_.index = this.plPart.length;
         _loc2_.snew = false;
         _loc2_.sel = 0;
         _loc2_.dead = false;
         _loc2_.useAs = 0;
         this.plPart.push(_loc2_);
         _shopItem.sel = 0;
      }
      
      public function addPartFromRepair(param1:int) : void
      {
         var _loc2_:F4DataOrg = this.repairPart[param1];
         _loc2_.sel = 0;
         _loc2_.dead = false;
      }
      
      public function addPartToShopFromPlayer(param1:int) : void
      {
         var _loc2_:F4DataOrg = this.plPart[param1];
         _loc2_.index = this.shopPart.length;
         _loc2_.sel = 0;
         _loc2_.dead = false;
         this.shopPart.push(_loc2_);
      }
      
      public function addPartToShop(param1:int) : void
      {
         var _loc2_:F4DataOrg = this.defPart[param1];
         _loc2_ = _loc2_.clone(this.defFire,this.defBullet);
         _loc2_.sel = 0;
         _loc2_.dead = false;
         _loc2_.index = this.shopPart.length;
         this.shopPart.push(_loc2_);
      }
      
      public function addPartFromDef(param1:int) : void
      {
         var _loc2_:F4DataOrg = this.defPart[param1];
         _loc2_ = _loc2_.clone(this.defFire,this.defBullet);
         _loc2_.index = this.plPart.length;
         this.plPart.push(_loc2_);
      }
      
      public function removePart(param1:F4DataOrg) : void
      {
         delete this.plPart[param1.index];
         this.reSortIndex(this.plPart);
      }
      
      public function removeOnesByArray(param1:Array, param2:Array) : void
      {
         var _loc3_:int = 0;
         var _loc4_:* = 0;
         var _loc5_:int = 0;
         for each(_loc3_ in param1)
         {
            this.removeFly(this.plFly[_loc3_]);
            this.plFly[_loc3_] = null;
         }
         _loc4_ = 0;
         while(_loc4_ < this.plFly.length)
         {
            if(this.plFly[_loc4_] == null)
            {
               this.plFly.splice(_loc4_);
               _loc4_--;
            }
            _loc4_++;
         }
         for each(_loc5_ in param2)
         {
            if(this.plPart[_loc5_])
            {
               this.removePart(this.plPart[_loc5_] as F4DataOrg);
            }
         }
      }
      
      public function removeFly(param1:F4DataLinks) : void
      {
         var _loc2_:F4DataHole = null;
         this.removePart(param1.f4dataorg);
         for each(_loc2_ in param1.f4dataorg)
         {
            if(_loc2_.partf4)
            {
               this.removeFly(_loc2_.partf4);
            }
         }
      }
      
      public function reSortIndex(param1:Array) : void
      {
         var _loc2_:int = 0;
         while(_loc2_ < param1.length)
         {
            param1[_loc2_].index = _loc2_;
            _loc2_++;
         }
      }
      
      public function reSortRPIndex(param1:Array) : void
      {
         var _loc2_:int = 0;
         while(_loc2_ < param1.length)
         {
            param1[_loc2_].rpindex = _loc2_;
            _loc2_++;
         }
      }
      
      public function save() : ByteArray
      {
         var _loc2_:F4DataBullet = null;
         var _loc3_:F4DataFire = null;
         var _loc4_:F4DataOrg = null;
         var _loc5_:F4DataLinks = null;
         var _loc6_:ByteArray = null;
         var _loc1_:Object = new Object();
         _loc1_["part"] = new Object();
         for each(_loc4_ in this.plPart)
         {
            _loc1_["part"]["" + _loc4_.index] = _loc4_.writeObj();
         }
         _loc1_["fly"] = new Object();
         for each(_loc5_ in this.plFly)
         {
            _loc1_["fly"]["" + _loc5_.index] = _loc5_.writeObj();
         }
         _loc6_ = new ByteArray();
         _loc6_.writeObject(_loc1_);
         _loc6_.compress();
         return _loc6_;
      }
      
      public function load(param1:ByteArray) : void
      {
         var _loc2_:Object = null;
         var _loc4_:* = undefined;
         var _loc5_:F4DataBullet = null;
         var _loc6_:F4DataFire = null;
         var _loc7_:F4DataOrg = null;
         var _loc8_:F4DataLinks = null;
         var _loc3_:ByteArray = new ByteArray();
         _loc3_.writeBytes(param1);
         _loc3_.position = 0;
         _loc3_.uncompress();
         _loc3_.position = 0;
         _loc2_ = _loc3_.readObject();
         this.plPart.length = 0;
         this.plFly.length = 0;
         for(_loc4_ in _loc2_["part"])
         {
            _loc7_ = new F4DataOrg();
            _loc7_.readFromObj(_loc2_["part"][_loc4_],this.defFire,this.defBullet,false);
            this.plPart[_loc7_.index] = _loc7_;
         }
         for(_loc4_ in _loc2_["fly"])
         {
            _loc8_ = new F4DataLinks();
            _loc8_.readFromObj(_loc2_["fly"][_loc4_],this.plPart,null,null);
            this.plFly[_loc8_.index] = _loc8_;
         }
      }
      
      public function saveSO() : ByteArray
      {
         var _loc2_:F4DataOrg = null;
         var _loc3_:F4DataLinks = null;
         var _loc4_:ByteArray = null;
         var _loc1_:Object = new Object();
         _loc1_["partShop"] = new Object();
         for each(_loc2_ in this.shopPart)
         {
            _loc1_["partShop"]["" + _loc2_.index] = _loc2_.writeObj();
         }
         _loc1_["part"] = new Object();
         for each(_loc2_ in this.plPart)
         {
            _loc1_["part"]["" + _loc2_.index] = _loc2_.writeObj();
         }
         _loc1_["fly"] = new Object();
         for each(_loc3_ in this.plFly)
         {
            _loc1_["fly"]["" + _loc3_.index] = _loc3_.writeObj();
         }
         _loc4_ = new ByteArray();
         _loc4_.writeObject(_loc1_);
         _loc4_.compress();
         return _loc4_;
      }
      
      public function loadSO(param1:ByteArray) : void
      {
         var _loc2_:Object = null;
         var _loc4_:* = undefined;
         var _loc5_:F4DataOrg = null;
         var _loc6_:F4DataLinks = null;
         var _loc3_:ByteArray = new ByteArray();
         _loc3_.writeBytes(param1);
         _loc3_.position = 0;
         _loc3_.uncompress();
         _loc3_.position = 0;
         _loc2_ = _loc3_.readObject();
         this.shopPart.length = 0;
         this.plPart.length = 0;
         this.plFly.length = 0;
         for(_loc4_ in _loc2_["partShop"])
         {
            _loc5_ = new F4DataOrg();
            _loc5_.readFromObj(_loc2_["partShop"][_loc4_],this.defFire,this.defBullet,false);
            this.shopPart[_loc5_.index] = _loc5_;
         }
         for(_loc4_ in _loc2_["part"])
         {
            _loc5_ = new F4DataOrg();
            _loc5_.readFromObj(_loc2_["part"][_loc4_],this.defFire,this.defBullet,false);
            this.plPart[_loc5_.index] = _loc5_;
         }
         for(_loc4_ in _loc2_["fly"])
         {
            _loc6_ = new F4DataLinks();
            _loc6_.readFromObj(_loc2_["fly"][_loc4_],this.plPart,null,null);
            this.plFly[_loc6_.index] = _loc6_;
         }
      }
      
      public function readDefFlyNow() : void
      {
         var _loc1_:F4DataLinks = null;
         var _loc3_:* = undefined;
         this.defFly.length = 0;
         var _loc2_:Object = Fly4UI.DEFFLYDATA;
         for(_loc3_ in _loc2_["flyDef"])
         {
            _loc1_ = new F4DataLinks();
            _loc1_.readFromObj(_loc2_["flyDef"][_loc3_],this.defPart,this.defFire,this.defBullet);
            this.defFly[_loc1_.index] = _loc1_;
         }
      }
      
      public function get onlyPlFly() : Boolean
      {
         var _loc2_:F4DataLinks = null;
         var _loc1_:int = 0;
         for each(_loc2_ in this.plFly)
         {
            if(++_loc1_ > 1)
            {
               return false;
            }
         }
         return true;
      }
   }
}

