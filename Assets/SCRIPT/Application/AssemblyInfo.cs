using System.Runtime.CompilerServices;

// Transitional friends while callers are still in Unity's predefined runtime
// assembly. Remove Assembly-CSharp once the corresponding application use cases
// have moved behind typed HOL.Application entry points.
[assembly: InternalsVisibleTo("Assembly-CSharp")]
[assembly: InternalsVisibleTo("HOL.EditModeTests")]
