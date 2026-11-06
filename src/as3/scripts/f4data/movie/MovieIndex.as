package f4data.movie
{
   public class MovieIndex
   {
      
      public var movIndex:Object;
      
      public var movData:Object;
      
      public function MovieIndex()
      {
         super();
         this.initIndex();
      }
      
      public function initIndex() : void
      {
         this.movIndex = [];
         this.movIndex[0] = [[0,1,2,3,4,5]];
         this.movIndex[3] = [null,[6,7,8]];
         this.movData = new Object();
      }
   }
}

