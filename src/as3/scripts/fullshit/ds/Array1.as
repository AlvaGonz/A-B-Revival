package fullshit.ds
{
   public class Array1
   {
      
      protected var _array:Array;
      
      public function Array1()
      {
         super();
         this._array = new Array();
      }
      
      public function addOne1(param1:AbsOne) : void
      {
         this._array.push(param1);
      }
      
      public function removeOne1(param1:AbsOne) : void
      {
         var _loc2_:int = this._array.indexOf(param1);
         if(_loc2_ != -1)
         {
            this._array.splice(_loc2_,1);
         }
      }
      
      public function run1() : void
      {
         var _loc4_:AbsOne = null;
         var _loc1_:int = 0;
         var _loc2_:int = int(this._array.length);
         var _loc3_:int = 0;
         while(_loc1_ < _loc2_)
         {
            _loc4_ = this._array[_loc1_];
            if(_loc4_.isDeadA1)
            {
               _loc4_.startRemoveA1();
               this._array[_loc1_] = this._array[--_loc2_];
               _loc3_++;
            }
            else
            {
               _loc4_.runA1();
               _loc1_++;
            }
         }
         var _loc5_:int = int(this._array.length);
         if(_loc3_ > 0)
         {
            _loc1_ = -1;
            while(++_loc1_ < _loc3_)
            {
               this._array[_loc2_ + _loc1_] = this._array[_loc5_ - 1 - _loc1_];
            }
         }
         this._array.length = _loc5_ - _loc3_;
      }
      
      public function removeSelf1() : void
      {
         var _loc1_:int = -1;
         var _loc2_:int = int(this._array.length);
         while(++_loc1_ < _loc2_)
         {
            (this._array[_loc1_] as AbsOne).startRemoveA1();
         }
         this._array.length = 0;
      }
      
      public function fastRemoveSelf1() : void
      {
         this._array.length = 0;
      }
      
      public function get num() : int
      {
         return this._array.length;
      }
      
      public function get lastOne() : AbsOne
      {
         if(this._array.length > 0)
         {
            return this._array[this._array.length - 1];
         }
         return null;
      }
      
      public function get random() : AbsOne
      {
         return this._array[int(this._array.length * Math.random())];
      }
      
      public function get array() : Array
      {
         return this._array;
      }
   }
}

