//------------------------------------------------------------------------------
// <auto-generated />
//
// This file was automatically generated by SWIG (http://www.swig.org).
// Version 3.0.10
//
// Do not make changes to this file unless you know what you are doing--modify
// the SWIG interface file instead.
//------------------------------------------------------------------------------

namespace ogmaneo {

public class Resources : global::System.IDisposable {
  private global::System.Runtime.InteropServices.HandleRef swigCPtr;
  private bool swigCMemOwnBase;

  internal Resources(global::System.IntPtr cPtr, bool cMemoryOwn) {
    swigCMemOwnBase = cMemoryOwn;
    swigCPtr = new global::System.Runtime.InteropServices.HandleRef(this, cPtr);
  }

  internal static global::System.Runtime.InteropServices.HandleRef getCPtr(Resources obj) {
    return (obj == null) ? new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero) : obj.swigCPtr;
  }

  ~Resources() {
    Dispose();
  }

  public virtual void Dispose() {
    lock(this) {
      if (swigCPtr.Handle != global::System.IntPtr.Zero) {
        if (swigCMemOwnBase) {
          swigCMemOwnBase = false;
          csogmaneoPINVOKE.delete_Resources(swigCPtr);
        }
        swigCPtr = new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero);
      }
      global::System.GC.SuppressFinalize(this);
    }
  }

  public Resources() : this(csogmaneoPINVOKE.new_Resources__SWIG_0(), true) {
  }

  public Resources(ComputeSystem.DeviceType type, int platformIndex, int deviceIndex) : this(csogmaneoPINVOKE.new_Resources__SWIG_1((int)type, platformIndex, deviceIndex), true) {
  }

  public Resources(ComputeSystem.DeviceType type, int platformIndex) : this(csogmaneoPINVOKE.new_Resources__SWIG_2((int)type, platformIndex), true) {
  }

  public Resources(ComputeSystem.DeviceType type) : this(csogmaneoPINVOKE.new_Resources__SWIG_3((int)type), true) {
  }

  public void create(ComputeSystem.DeviceType type, int platformIndex, int deviceIndex) {
    csogmaneoPINVOKE.Resources_create__SWIG_0(swigCPtr, (int)type, platformIndex, deviceIndex);
    if (csogmaneoPINVOKE.SWIGPendingException.Pending) throw csogmaneoPINVOKE.SWIGPendingException.Retrieve();
  }

  public void create(ComputeSystem.DeviceType type, int platformIndex) {
    csogmaneoPINVOKE.Resources_create__SWIG_1(swigCPtr, (int)type, platformIndex);
    if (csogmaneoPINVOKE.SWIGPendingException.Pending) throw csogmaneoPINVOKE.SWIGPendingException.Retrieve();
  }

  public void create(ComputeSystem.DeviceType type) {
    csogmaneoPINVOKE.Resources_create__SWIG_2(swigCPtr, (int)type);
    if (csogmaneoPINVOKE.SWIGPendingException.Pending) throw csogmaneoPINVOKE.SWIGPendingException.Retrieve();
  }

  public ComputeSystem getComputeSystem() {
    global::System.IntPtr cPtr = csogmaneoPINVOKE.Resources_getComputeSystem(swigCPtr);
    ComputeSystem ret = (cPtr == global::System.IntPtr.Zero) ? null : new ComputeSystem(cPtr, true);
    if (csogmaneoPINVOKE.SWIGPendingException.Pending) throw csogmaneoPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public SWIGTYPE_p_std__unordered_mapT_std__string_std__shared_ptrT_ogmaneo__ComputeProgram_t_t getPrograms() {
    SWIGTYPE_p_std__unordered_mapT_std__string_std__shared_ptrT_ogmaneo__ComputeProgram_t_t ret = new SWIGTYPE_p_std__unordered_mapT_std__string_std__shared_ptrT_ogmaneo__ComputeProgram_t_t(csogmaneoPINVOKE.Resources_getPrograms(swigCPtr), false);
    if (csogmaneoPINVOKE.SWIGPendingException.Pending) throw csogmaneoPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

}

}
