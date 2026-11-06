package f4.unit.parts
{
   import f4.data.InitData;
   import f4.unit.ai.AIPart;
   import f4.unit.ai.AIUnity;
   import f4.unit.fire.FireMode;
   import f4.unit.fire.FirePort;
   import f4.unit.move.MoveAs;
   import f4.unit.upgrade.FlyData;
   import f4data.sdata.ConfigAndStory;
   import flash.geom.Point;
   import fullshit.ds.AbsOne;
   
   public class Unity extends AbsOne
   {
      
      public var parts:Array;
      
      public var move:MoveAs;
      
      public var name:String;
      
      public var udata:FlyData;
      
      public var friend:Boolean;
      
      public var minLifePart:AIPart;
      
      public var index:int;
      
      public var autoHP:Number;
      
      public var autoHT:int;
      
      public var powrate:Number;
      
      public var liferate:Number;
      
      public var powNum:int;
      
      public var gunNum:int;
      
      public function Unity(param1:String, param2:MoveAs)
      {
         super();
         this.minLifePart = null;
         this.move = param2;
         this.name = param1;
         this.parts = [];
         this.udata = new FlyData("flywhat",new Point(),new Point(),new Point(),0,0);
      }
      
      public function linkPart() : void
      {
         var _loc1_:AIPart = null;
         this.runAutoHeal();
         this.gunNum = this.powNum = 0;
         this.udata.hp.x = this.udata.turn.x = this.udata.movepow.x = this.udata.mass = 0;
         for each(_loc1_ in this.parts)
         {
            this.udata.hp.x += _loc1_.part.data.hp.x;
            this.udata.turn.x += _loc1_.part.data.turn.y * this.powrate;
            this.udata.movepow.x += _loc1_.part.data.movepow.y * this.powrate;
            this.udata.mass += _loc1_.part.data.mass;
            if(_loc1_.org.type == 2)
            {
               ++this.powNum;
            }
            else if(_loc1_.org.type == 3)
            {
               ++this.gunNum;
            }
         }
      }
      
      public function linkPartFirst() : void
      {
         var _loc1_:AIPart = null;
         this.gunNum = this.powNum = 0;
         this.udata.hp.x = this.udata.hp.y = this.udata.turn.x = this.udata.movepow.x = this.udata.mass = 0;
         for each(_loc1_ in this.parts)
         {
            _loc1_.part.data.hp.x *= this.liferate;
            _loc1_.part.data.hp.y = _loc1_.part.data.hp.x;
            this.udata.hp.y += _loc1_.part.data.hp.y;
            this.udata.hp.x += _loc1_.part.data.hp.x;
            this.udata.turn.x += _loc1_.part.data.turn.y * this.powrate;
            this.udata.movepow.x += _loc1_.part.data.movepow.y * this.powrate;
            this.udata.mass += _loc1_.part.data.mass;
            if(_loc1_.org.type == 2)
            {
               ++this.powNum;
            }
            else if(_loc1_.org.type == 3)
            {
               ++this.gunNum;
            }
         }
      }
      
      public function flyPower(param1:Number) : void
      {
         var _loc2_:Number = this.udata.movepow.x / this.udata.mass * param1;
         this.move.acc.x = _loc2_;
         this.move.acc.y = this.move.radFace;
         this.move.r2d(this.move.acc,this.move.accp);
      }
      
      public function linkFireMode(param1:AIUnity) : void
      {
         var _loc2_:AIPart = null;
         var _loc3_:FireMode = null;
         var _loc4_:FirePort = null;
         for each(_loc2_ in this.parts)
         {
            for each(_loc4_ in _loc2_.part.firePorts.ports)
            {
               for each(_loc3_ in _loc4_.fireModes)
               {
                  _loc3_.unity = param1;
               }
            }
         }
      }
      
      public function linkFriend(param1:Boolean, param2:Boolean) : void
      {
         var _loc7_:AIPart = null;
         var _loc8_:FireMode = null;
         var _loc9_:FirePort = null;
         var _loc3_:Number = 1.6;
         var _loc4_:Number = 1.45;
         var _loc5_:Number = 1.685;
         var _loc6_:Number = 1.425;
         var _loc10_:Number = 3;
         var _loc11_:Number = 1;
         var _loc12_:Number = 1;
         var _loc13_:Number = 0.85;
         var _loc14_:Number = 1;
         if(ConfigAndStory.STORY == 0)
         {
            _loc12_ = 0.61;
         }
         else if(ConfigAndStory.STORY < 8)
         {
            _loc12_ = 0.88;
            _loc11_ = 10;
            _loc14_ = 0.52;
            _loc13_ = 0.1;
         }
         else if(ConfigAndStory.STORY < 15)
         {
            _loc12_ = 0.85;
            _loc11_ = 6;
            _loc14_ = 0.9;
            _loc13_ = 0.5;
         }
         else if(ConfigAndStory.STORY < 18)
         {
            _loc12_ = 0.84;
            _loc11_ = 3;
            _loc14_ = 1.5;
         }
         else if(ConfigAndStory.STORY < 35)
         {
            _loc11_ = 1;
            _loc13_ = 1;
            _loc12_ = 0.72;
            _loc4_ = 1.6;
            _loc6_ = 1.6;
            _loc10_ = 1.8;
            _loc14_ = 2;
         }
         else
         {
            _loc13_ = 1;
            _loc11_ = 0.5;
            _loc12_ = 0.825;
            _loc4_ = 2;
            _loc6_ = 1.8;
            _loc10_ = 1.5;
            _loc14_ = 3;
         }
         if(ConfigAndStory.STORY == 7)
         {
            _loc14_ = 1;
         }
         else if(ConfigAndStory.STORY == 9)
         {
            _loc14_ = 1;
         }
         else if(ConfigAndStory.STORY == 10)
         {
            _loc11_ = 1.5;
            _loc13_ = 0.24;
         }
         for each(_loc7_ in this.parts)
         {
            for each(_loc9_ in _loc7_.part.firePorts.ports)
            {
               for each(_loc8_ in _loc9_.fireModes)
               {
                  _loc8_.friend = param1;
                  if(param2)
                  {
                     _loc8_.powdmg = 1.2;
                     _loc8_.powspd = InitData.powspdPlayer * _loc3_;
                     _loc8_.powrate = InitData.powratePlayer;
                     _loc8_.fireTimer.stopFrame = Math.ceil(_loc8_.fireTimer.stopFrame / _loc8_.powrate);
                  }
                  else if(param1)
                  {
                     _loc8_.powdmg = _loc13_;
                     _loc8_.powspd = InitData.powspdAI * _loc3_;
                     _loc8_.powrate = InitData.powrateAI;
                     _loc8_.fireTimer.stopFrame = Math.ceil(_loc8_.fireTimer.stopFrame / _loc8_.powrate) * _loc11_;
                  }
                  else
                  {
                     _loc8_.powdmg = _loc12_;
                     _loc8_.powspd = InitData.powspdEnemy * _loc4_;
                     _loc8_.powrate = InitData.powrateEnemy;
                     _loc8_.fireTimer.stopFrame = Math.ceil(_loc8_.fireTimer.stopFrame / _loc8_.powrate) * _loc10_;
                  }
               }
            }
         }
         this.friend = param1;
         if(param2)
         {
            this.powrate = InitData.powratePlayer * _loc5_;
            this.liferate = InitData.lifePlayer;
         }
         else if(param1)
         {
            this.powrate = InitData.powrateAI * _loc5_;
            this.liferate = InitData.lifeAI;
         }
         else
         {
            this.powrate = InitData.powrateEnemy * _loc6_;
            this.liferate = InitData.lifeEnemy * _loc14_;
         }
         if(ConfigAndStory.STORY == 49)
         {
            this.powrate *= 0.71;
         }
      }
      
      public function buildHitRect() : void
      {
         var _loc5_:AIPart = null;
         var _loc1_:Number = int.MAX_VALUE;
         var _loc2_:Number = int.MIN_VALUE;
         var _loc3_:Number = int.MAX_VALUE;
         var _loc4_:Number = int.MIN_VALUE;
         for each(_loc5_ in this.parts)
         {
            if(_loc5_.part.partMove.hitRect.x < _loc1_)
            {
               _loc1_ = _loc5_.part.partMove.hitRect.x;
            }
            if(_loc5_.part.partMove.hitRect.x + _loc5_.part.partMove.hitRect.width > _loc2_)
            {
               _loc2_ = _loc5_.part.partMove.hitRect.x + _loc5_.part.partMove.hitRect.width;
            }
            if(_loc5_.part.partMove.hitRect.y < _loc3_)
            {
               _loc3_ = _loc5_.part.partMove.hitRect.y;
            }
            if(_loc5_.part.partMove.hitRect.y + _loc5_.part.partMove.hitRect.height > _loc4_)
            {
               _loc4_ = _loc5_.part.partMove.hitRect.y + _loc5_.part.partMove.hitRect.height;
            }
         }
         this.move.hitRect.x = _loc1_;
         this.move.hitRect.y = _loc3_;
         this.move.hitRect.width = _loc2_ - _loc1_;
         this.move.hitRect.height = _loc4_ - _loc3_;
      }
      
      public function runAutoHeal() : void
      {
         var _loc1_:AIPart = null;
         if(this.autoHT > 0)
         {
            --this.autoHT;
            for each(_loc1_ in this.parts)
            {
               _loc1_.autoHeal(this.autoHP);
            }
         }
      }
   }
}

