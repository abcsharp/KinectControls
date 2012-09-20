using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;
using Microsoft.Kinect;

namespace KinectControls
{
	public enum Orientation
	{
		Vertical,
		Horizontal
	}

	public class JointSlider
	{
		private int maximum;

		protected KinectSensor sensor;

		protected void HitTest(object sender,SkeletonFrameReadyEventArgs e)
		{
			if(IsEnabled)
				using(SkeletonFrame skeletonFrame=e.OpenSkeletonFrame())
					if(skeletonFrame!=null){
						Skeleton[] skeletons=new Skeleton[skeletonFrame.SkeletonArrayLength];
						skeletonFrame.CopySkeletonDataTo(skeletons);
						Skeleton firstTrackedSkeleton=null;
						foreach(Skeleton skel in skeletons)
							if(skel.TrackingState==SkeletonTrackingState.Tracked){
 								firstTrackedSkeleton=skel;
								break;
							}
						if(firstTrackedSkeleton==null){
							IsTracking=false;
							IsHitting=false;
							return;
						}
						int hittingJointCount=0;
						JointType firstHittingJoint=JointType.Head;
						DepthImagePoint firstHittingPoint=new DepthImagePoint();
						foreach(JointType joint in Joints){
							var jointPoint=sensor.MapSkeletonPointToDepth(firstTrackedSkeleton.Joints[joint].Position,DepthImageFormat.Resolution640x480Fps30);
							var HitTestResult=jointPoint.X>=SliderBounds.Left&&jointPoint.X<=SliderBounds.Right&&jointPoint.Y>=SliderBounds.Top&&jointPoint.Y<=SliderBounds.Bottom;
							if(HitTestResult){
								if(hittingJointCount==0){
									firstHittingJoint=joint;
									firstHittingPoint=jointPoint;
								}
								hittingJointCount++;
							}
						}
						if(hittingJointCount>0&&!IsHitting){
							IsHitting=true;
							OnHitting(new JointHittingEventArgs(firstHittingJoint));
						}else if(hittingJointCount==0&&IsHitting){
							IsHitting=false;
							OnLeave();
						}
						if(IsHitting){
							if(Orientation==Orientation.Horizontal){
								var sliderValue=((firstHittingPoint.X-SliderBounds.Left)/SliderBounds.Width)*Maximum;
								Value=(int)sliderValue;
							}else{
								var sliderValue=((SliderBounds.Height-firstHittingPoint.Y+SliderBounds.Top)/SliderBounds.Height)*Maximum;
								Value=(int)sliderValue;
							}
						}
					}
			return;
		}

		protected void OnHitting(JointHittingEventArgs e)
		{
			if(e!=null&&JointHitting!=null) JointHitting(this,e);
			return;
		}

		protected void OnLeave()
		{
			if(JointLeave!=null) JointLeave(this,new EventArgs());
			return;
		}

		public bool IsEnabled{get;set;}
		public bool IsTracking{get;protected set;}
		public bool IsHitting{get;protected set;}
		public Rect SliderBounds{get;set;}
		public Orientation Orientation{get;set;}
		public IEnumerable<JointType> Joints{get;set;}
		public int Value{get;protected set;}
		public int Maximum{
			get{return maximum;}
			set{
				if(value<=0) throw new ArgumentOutOfRangeException();
				else maximum=value;
			}
		}
		public event EventHandler<JointHittingEventArgs> JointHitting;
		public event EventHandler JointLeave;

		public JointSlider(KinectSensor sensor,IEnumerable<JointType> joints,int max,Rect sliderBounds)
		{
			if(sensor==null) throw new ArgumentNullException();
			this.sensor=sensor;
			sensor.SkeletonFrameReady+=HitTest;
			if(joints==null) Joints=new List<JointType>();
			else Joints=joints;
			Value=0;
			Maximum=max;
			if(sliderBounds==null) throw new ArgumentNullException();
			SliderBounds=sliderBounds;
			Orientation=Orientation.Horizontal;
			return;
		}
	}
}
