package crc642c3bad5b1fa407ec;


public class ScannerV2Page_NativeMlKitCameraController_BarcodeSuccessListener
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.google.android.gms.tasks.OnSuccessListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onSuccess:(Ljava/lang/Object;)V:GetOnSuccess_Ljava_lang_Object_Handler:Android.Gms.Tasks.IOnSuccessListenerInvoker, Xamarin.GooglePlayServices.Tasks\n" +
			"";
		mono.android.Runtime.register ("InventoryScanner.Views.ScannerV2Page+NativeMlKitCameraController+BarcodeSuccessListener, InventoryScanner", ScannerV2Page_NativeMlKitCameraController_BarcodeSuccessListener.class, __md_methods);
	}

	public ScannerV2Page_NativeMlKitCameraController_BarcodeSuccessListener ()
	{
		super ();
		if (getClass () == ScannerV2Page_NativeMlKitCameraController_BarcodeSuccessListener.class) {
			mono.android.TypeManager.Activate ("InventoryScanner.Views.ScannerV2Page+NativeMlKitCameraController+BarcodeSuccessListener, InventoryScanner", "", this, new java.lang.Object[] {  });
		}
	}

	public void onSuccess (java.lang.Object p0)
	{
		n_onSuccess (p0);
	}

	private native void n_onSuccess (java.lang.Object p0);

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
