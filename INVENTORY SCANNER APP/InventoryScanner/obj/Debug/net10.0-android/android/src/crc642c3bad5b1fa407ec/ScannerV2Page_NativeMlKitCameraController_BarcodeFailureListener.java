package crc642c3bad5b1fa407ec;


public class ScannerV2Page_NativeMlKitCameraController_BarcodeFailureListener
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.google.android.gms.tasks.OnFailureListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onFailure:(Ljava/lang/Exception;)V:GetOnFailure_Ljava_lang_Exception_Handler:Android.Gms.Tasks.IOnFailureListenerInvoker, Xamarin.GooglePlayServices.Tasks\n" +
			"";
		mono.android.Runtime.register ("InventoryScanner.Views.ScannerV2Page+NativeMlKitCameraController+BarcodeFailureListener, InventoryScanner", ScannerV2Page_NativeMlKitCameraController_BarcodeFailureListener.class, __md_methods);
	}

	public ScannerV2Page_NativeMlKitCameraController_BarcodeFailureListener ()
	{
		super ();
		if (getClass () == ScannerV2Page_NativeMlKitCameraController_BarcodeFailureListener.class) {
			mono.android.TypeManager.Activate ("InventoryScanner.Views.ScannerV2Page+NativeMlKitCameraController+BarcodeFailureListener, InventoryScanner", "", this, new java.lang.Object[] {  });
		}
	}

	public void onFailure (java.lang.Exception p0)
	{
		n_onFailure (p0);
	}

	private native void n_onFailure (java.lang.Exception p0);

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
