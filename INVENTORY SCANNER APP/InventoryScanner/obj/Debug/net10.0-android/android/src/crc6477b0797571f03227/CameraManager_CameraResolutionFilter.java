package crc6477b0797571f03227;


public class CameraManager_CameraResolutionFilter
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		androidx.camera.core.resolutionselector.ResolutionFilter
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_filter:(Ljava/util/List;I)Ljava/util/List;:GetFilter_Ljava_util_List_IHandler:AndroidX.Camera.Core.ResolutionSelector.IResolutionFilterInvoker, Xamarin.AndroidX.Camera.Core\n" +
			"";
		mono.android.Runtime.register ("ZXing.Net.Maui.CameraManager+CameraResolutionFilter, ZXing.Net.MAUI", CameraManager_CameraResolutionFilter.class, __md_methods);
	}

	public CameraManager_CameraResolutionFilter ()
	{
		super ();
		if (getClass () == CameraManager_CameraResolutionFilter.class) {
			mono.android.TypeManager.Activate ("ZXing.Net.Maui.CameraManager+CameraResolutionFilter, ZXing.Net.MAUI", "", this, new java.lang.Object[] {  });
		}
	}

	public java.util.List filter (java.util.List p0, int p1)
	{
		return n_filter (p0, p1);
	}

	private native java.util.List n_filter (java.util.List p0, int p1);

	private java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
