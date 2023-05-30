using System;
using System.Windows.Forms;
using Tekla.Structures.Geometry3d;
using Tekla.Structures.Model;
using Tekla.Structures.Model.Operations;
using TSMUI = Tekla.Structures.Model.UI;


namespace Copy_Multiple_End
{
    public partial class Form1Tek1 : Form
    {
        public Form1Tek1()
        {
            InitializeComponent();
            comboBox1.SelectedIndex = 0;
        }

        //Initializing a model object that establishes a connection between Tekla and our code
        Model myModel = new Model();

        //Code to verify the connectivity of our model
        private void button1_Click(object sender, EventArgs e)
        {
            if(myModel.GetConnectionStatus())
            {
                MessageBox.Show("Model Connected");
            }
            else
            {
                MessageBox.Show("Model not Connected");
            }
        }

        //This method duplicates the model objects from one part to another part
        public void CopyMultiple()
        {
            //The initialization of the Picker class and ModelObjectSelector class
            TSMUI.Picker picker = new TSMUI.Picker();
            TSMUI.ModelObjectSelector modelObjectSelector = new TSMUI.ModelObjectSelector();

            //Initializing an enumerator for objects to be copied and destination objects
            ModelObjectEnumerator objectsToBeCopied = modelObjectSelector.GetSelectedObjects();
            ModelObject sourceObject = null;
            ModelObjectEnumerator destinationObjects = null;
            int position = comboBox1.SelectedIndex;

            //Checking if any object is selected to be copied
            if (objectsToBeCopied.GetSize() == 0)
            {
                Operation.DisplayPrompt("You have to select objects before copying!");
                goto End;
            }

            //Initiating a prompt to pick a source object and multiple destination objects
            try
            {
                sourceObject = picker.PickObject(TSMUI.Picker.PickObjectEnum.PICK_ONE_PART, "Pick source part");
                destinationObjects = picker.PickObjects(TSMUI.Picker.PickObjectsEnum.PICK_N_PARTS, "Pick destination objects, can be multiple objects as well");
            }
            catch (Exception)
            {
                Operation.DisplayPrompt("User interrupt");
            }

            //The main loop that copies each object to every destination object
            try
            {
                if (destinationObjects != null)
                {
                    //Getting the coordinate system of the source object
                    CoordinateSystem sourceCoordinationSystem = sourceObject.GetCoordinateSystem();

                    while (destinationObjects.MoveNext())
                    {
                        CoordinateSystem destionationCoordinateSystem = null;
                        destinationObjects.Current.Select();
                        sourceObject.Select();
                        //If the selected index is 0, then it will execute using the part's coordinate system
                        //If the selected index is 1, then the code will execute using the part's end coordinate system
                        if (position == 1 && sourceObject is Beam && destinationObjects.Current is Beam)
                        {
                            destionationCoordinateSystem = EndCoordinateSystem(destinationObjects.Current as Part);
                            sourceCoordinationSystem = EndCoordinateSystem(sourceObject as Part);
                        }
                        else
                        {
                            destionationCoordinateSystem = destinationObjects.Current.GetCoordinateSystem();
                            sourceCoordinationSystem = sourceObject.GetCoordinateSystem();
                        }

                        //The main loop that copies objects using the CopyObject method
                        while (objectsToBeCopied.MoveNext())
                        {
                            if (destinationObjects.Current.Identifier.GUID != sourceObject.Identifier.GUID)
                            {
                                var copiedObject = Operation.CopyObject(objectsToBeCopied.Current, sourceCoordinationSystem, destionationCoordinateSystem);
                            }

                        }
                        objectsToBeCopied.Reset();
                    }
                }
            }
            catch (Exception)
            {
                Operation.DisplayPrompt("Unknown error 1");
                goto End;
            }
            myModel.CommitChanges();
            Operation.DisplayPrompt("Operation Completed");
        End:;
        }

        //The new method to determine the coordinate system at the end handle
        public CoordinateSystem EndCoordinateSystem(Part part)
        {
            Beam beam = part as Beam;
            CoordinateSystem source = part.GetCoordinateSystem();

            //This method is very useful compared to workplanes
            //While working with workplanes, it is not working properly as the program executes too fast
            Matrix ToLocal = MatrixFactory.ToCoordinateSystem(source);
            Point CurrentPoint = ToLocal.Transform(beam.EndPoint);

            //The beam endpoint aligns with the Y and Z axes of the part's coordinate system
            CurrentPoint.Y = 0;
            CurrentPoint.Z = 0;
            Matrix ToCurrentWP = MatrixFactory.FromCoordinateSystem(source);
            Point Origin = ToCurrentWP.Transform(CurrentPoint);
            CoordinateSystem endCoordinateSystem = new CoordinateSystem(Origin, source.AxisX, source.AxisY);
            return endCoordinateSystem;
        }

        //Initiate the Copy Multiple method when the operator clicks
        private void button2_Click(object sender, EventArgs e)
        {
            CopyMultiple();
        }

    }
}
