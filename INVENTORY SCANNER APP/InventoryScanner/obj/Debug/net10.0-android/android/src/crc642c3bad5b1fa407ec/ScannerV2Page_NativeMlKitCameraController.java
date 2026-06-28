package crc642c3bad5b1fa407ec;


public class ScannerV2Page_NativeMlKitCameraController
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		android.view.SurfaceHolder.Callback,
		android.hardware.Camera.PreviewCallback
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_surfaceChanged:(Landroid/view/SurfaceHolder;III)V:GetSurfaceChanged_Landroid_view_SurfaceHolder_IIIHandler:Android.Views.ISurfaceHolderCallbackInvoker, Mono.Android, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null\n" +
			"n_surfaceCreated:(Landroid/view/SurfaceHolder;)V:GetSurfaceCreated_Landroid_view_SurfaceHolder_Handler:Android.Views.ISurfaceHolderCallbackInvoker, Mono.Android, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null\n" +
			"n_surfaceDestroyed:(Landroid/view/SurfaceHolder;)V:GetSurfaceDestroyed_Landroid_view_SurfaceHolder_Handler:Android.Views.ISurfaceHolderCallbackInvoker, Mono.Android, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null\n" +
			"n_onPreviewFrame:([BLandroid/hardware/Camera;)V:GetOnPreviewFrame_arrayBLandroid_hardware_Camera_Handler:Android.Hardware.Camera+IPreviewCallbackInvoker, Mono.Android, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null\n" +
			"";
		mono.android.Runtime.register ("InventoryScanner.Views.ScannerV2Page+NativeMlKitCameraController, InventoryScanner", ScannerV2Page_NativeMlKitCameraController.class, __md_methods);
	}

	public ScannerV2Page_NativeMlKitCameraController ()
	{
		super ();
		if (getClass () == ScannerV2Page_NativeMlKitCameraController.class) {
			mono.android.TypeManager.Activate ("InventoryScanner.Views.ScannerV2Page+NativeMlKitCameraController, InventoryScanner", "", this, new java.lang.Object[] {  });
		}
	}

	public void surfaceChanged (android.view.SurfaceHolder p0, int p1, int p2, int p3)
	{
		n_surfaceChanged (p0, p1, p2, p3);
	}

	private native void n_surfaceChanged (android.view.SurfaceHolder p0, int p1, int p2, int p3);

	public void surfaceCreated (android.view.SurfaceHolder p0)
	{
		n_surfaceCreated (p0);
	}

	private native void n_surfaceCreated (android.view.SurfaceHolder p0);

	public void surfaceDestroyed (android.view.SurfaceHolder p0)
	{
		n_surfaceDestroyed (p0);
	}

	private native void n_surfaceDestroyed (android.view.SurfaceHolder p0);

	public void onPreviewFrame (byte[] p0, android.hardware.Camera p1)
	{
		n_onPreviewFrame (p0, p1);
	}

	private native void n_onPreviewFrame (byte[] p0, android.hardware.Camera p1);

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
