using System;
//using System.Windows.Forms;
using System.Collections.Generic;
using System.Text;
using NXOpen;
using NXOpenUI;
using NXOpen.UF;
using NXOpen.Utilities;
using NXOpen.Features;
using NXOpen.Assemblies;
using System.IO;
using NXOpen.Preferences;

namespace setpaz
{
    public class Class1
    {
        private static UI theUI;
        private static UFSession theUFSession;
        public static Tag targets;

        private static void setpaz_debug(string text, string index) 
        {

            using (System.IO.StreamWriter file1 = new System.IO.StreamWriter(@"debug" + index + ".txt"))
                {
                    file1.WriteLine(text);
                }
               
           
        }

        public static void setpaz_SetSelectionOption(int object_type, int object_subtype, int solid_type, out NXOpen.UF.UFUi.SelectionOption opts)
        {
            /*
             int object_type, int object_subtype, int solid_type
             body: 70 0  0 
             face: 71 22 20  //UF_face_type 71, UF_bounded_plane_subtype 22, UF_UI_SEL_FEATURE_ANY_FACE 20 
             
             Scope:
                0 - UF_UI_SEL_SCOPE_NO_CHANGE 
                1 - UF_UI_SEL_SCOPE_ANY_IN_ASSEMBLY 
                2 - UF_UI_SEL_SCOPE_WORK_PART 
                3 - UF_UI_SEL_SCOPE_WORK_PART_AND_OCC
            */

            NXOpen.UF.UFUi.Mask[] mask = new NXOpen.UF.UFUi.Mask[1];
            mask[0].object_type = object_type;
            mask[0].object_subtype = object_subtype;
            mask[0].solid_type = solid_type;
            
            // int unhighlight = 0;
            opts.other_options = 0;
            opts.reserved = new IntPtr();
            opts.num_mask_triples = 1;
            opts.mask_triples = mask;
            opts.scope = 3;  
        }

        public static void setpaz_GetObject(string type_object, UFSession theUFSes, UFUi.SelectionOption optss, out Tag target, out double[] cursor) 
        {
            theUI = UI.GetUI();
            string message = "Please select";
            int norm_dir, type, response; // response: 1 = Back, 2 = Cancel, 4 = Object selected by name, 5 = Object selected
            Tag view, find_obj;
            cursor = new double[3];
            double[] point = new double[3];
            double[] dir = new double[3];
            double[] box = new double[6];
            double radius, rad_data;
            

            target = 0;
            int i = 0;

            do
            {
                theUFSes.Ui.SelectSingle(message + " " + type_object, ref optss, out response, out find_obj, cursor, out view);
                if (type_object == "face")
                {
                    theUFSes.Modl.AskFaceData(find_obj, out type, point, dir, box, out radius, out rad_data, out norm_dir);
                    if (type == 22)
                    {
                        target = find_obj;
                        i = 1;
                    }
                }
                else if (type_object == "body") 
                {
                    theUFSes.Modl.AskBodyType(find_obj, out type);
                    if (type == 5601) 
                    {
                        target = find_obj;
                        i = 2;
                    }
                }
            }
            while (response != 5 && i!=0);

            if (response != 5)
            {

                theUI.NXMessageBox.Show("Select " + type_object, NXMessageBox.DialogType.Error, "The " + type_object + " don't selected!");
            }
        }

        public static void setpaz_AskSeatingFaceName(Tag target_face, out string[] seating)
        {
            string target_face_name = "";
            theUFSession.Obj.AskName(target_face, out target_face_name);
            //получить индекс блока из имени - split[1]
            string str = target_face_name;
            string[] split = str.Split(new Char[] { '_' });
            //получить оставшиеся 2 имени плоскости
            seating = new string[6];
            if (split[0] == "TOP" || split[0] == "SIDE" || split[0] == "FRONT")
            {
                seating[0] = "TOP_" + split[1] + "_0";
                seating[1] = "TOP_" + split[1] + "_1";
                seating[2] = "SIDE_" + split[1] + "_0";
                seating[3] = "SIDE_" + split[1] + "_1";
                seating[4] = "FRONT_" + split[1] + "_1";
                seating[5] = "FRONT_" + split[1] + "_0";
            }
        }

        public static void setpaz_AskFaceComponent(Tag occur, UFSession theUFSession, out NXOpen.Assemblies.Component component1)
        {
            Tag to_part_occ;
            Session theSession = Session.GetSession();
            Part workPart = theSession.Parts.Work;
            string part_name, refset_name, instance_name;
            double[] origin = new double[3];
            double[] csys_matrix = new double[9];
            double[,] transform = new double[4, 4];

            theUFSession.Assem.AskParentComponent(occur, out to_part_occ);
            Tag to_part_ins = theUFSession.Assem.AskInstOfPartOcc(to_part_occ);
            theUFSession.Assem.AskComponentData(to_part_occ, out part_name, out refset_name, out instance_name, origin, csys_matrix, transform);
            string path_to_part = Path.GetFileName(part_name);
            path_to_part = path_to_part.Substring(0, path_to_part.Length - 4);
            //theUI.NXMessageBox.Show("PAZ", NXMessageBox.DialogType.Error, refset_name + " >> " + path_to_part); // "MODEL >> Plita 7081-2202"
            component1 = (NXOpen.Assemblies.Component)workPart.ComponentAssembly.RootComponent.FindObject("COMPONENT " + path_to_part + " 1");
        }

        public static void setpaz_CheckSeatingFaceComponent(NXOpen.Assemblies.Component component1, string[] seating, out Face[] faces)
        {
            faces = new Face[6];
            //seating = new string[6];
            Face face;

            for (int j = 1; j < 168; j++)
            {
                try
                {
                    face = (Face)component1.FindObject("PROTO#.Features|UNPARAMETERIZED_FEATURE(0)|FACE " + j); //PROTO#.Features|UNPARAMETERIZED_FEATURE(0)|FACE
                    for (int i = 0; i <= 5; i++)
                    {
                        if (face.Name == seating[i]) 
                        {
                            faces[i] = face;
                        }
                    }
                }
                catch (NXOpen.NXException ex)
                {
                    // theUI.NXMessageBox.Show("PAZ", NXMessageBox.DialogType.Error, ex.Message);
                }
            }
        }

        public static void setpaz_CreateConstraint(UFSession theUFSession, NXOpen.Assemblies.Component component1, Face face_component1, NXOpen.Assemblies.Component component2, Face face_component2, string type, int distance)
        {
            //Get workPart
            Session theSession = Session.GetSession();
            Part workPart = theSession.Parts.Work;

            //Create Positioner
            NXOpen.Positioning.ComponentPositioner componentPositioner1;
            componentPositioner1 = workPart.ComponentAssembly.Positioner;
            componentPositioner1.ClearNetwork();
            componentPositioner1.BeginAssemblyConstraints();

            //Ask Network
            NXOpen.Positioning.Network network1;
            network1 = componentPositioner1.EstablishNetwork();
            NXOpen.Positioning.ComponentNetwork componentNetwork1 = (NXOpen.Positioning.ComponentNetwork)network1;
            componentNetwork1.MoveObjectsState = true;
            NXOpen.Assemblies.Component nullAssemblies_Component = null;
            componentNetwork1.DisplayComponent = nullAssemblies_Component;
            componentNetwork1.NetworkArrangementsMode = NXOpen.Positioning.ComponentNetwork.ArrangementsMode.Existing;

            //Create constraint
            NXOpen.Positioning.Constraint constraint1;
            constraint1 = componentPositioner1.CreateConstraint();

            //Select type Constraint
            NXOpen.Positioning.ComponentConstraint componentConstraint1 = (NXOpen.Positioning.ComponentConstraint)constraint1;
            if (type == "Touch Align")
            {
                componentConstraint1.ConstraintAlignment = NXOpen.Positioning.Constraint.Alignment.ContraAlign;
                componentConstraint1.ConstraintType = NXOpen.Positioning.Constraint.Type.Touch;
            }
            else if (type == "Distance") 
            {
                componentConstraint1.ConstraintType = NXOpen.Positioning.Constraint.Type.Distance;
                //componentConstraint1.SetExpression(distance.ToString());
            }
            //Create constraint reference Componen1 + Face1
            NXOpen.Positioning.ConstraintReference constraintReference1;
            constraintReference1 = componentConstraint1.CreateConstraintReference(component1, face_component1, false, false);
            //set help point
            Point3d helpPoint1 = new Point3d(0, 0, 0);
            setpaz_SetHelpPoint(theUFSession, face_component1.Tag, out helpPoint1);
            constraintReference1.HelpPoint = helpPoint1;
            
            //Create constraint reference Componen2 + Face1
            NXOpen.Positioning.ConstraintReference constraintReference2;
            constraintReference2 = componentConstraint1.CreateConstraintReference(component2, face_component2, false, false);
            //set help point
            Point3d helpPoint2 = new Point3d(0, 0, 0);
            setpaz_SetHelpPoint(theUFSession, face_component1.Tag, out helpPoint2);
            constraintReference2.HelpPoint = helpPoint2;
            
            constraintReference2.SetFixHint(true);
            
            //Update constraint params
            if (type == "Touch Align")
            {
                componentConstraint1.SetAlignmentHint(NXOpen.Positioning.Constraint.Alignment.ContraAlign);
            } 
            else if (type == "Distance")
            {
                componentConstraint1.SetExpression(distance.ToString());
            }

           
           

            //Solve Network
            componentNetwork1.Solve();
            

            //Update Network
            componentNetwork1.ResetDisplay();
            componentNetwork1.ApplyToModel();

            //Update Positioner
            componentPositioner1.ClearNetwork();
            componentPositioner1.DeleteNonPersistentConstraints();
            NXOpen.Assemblies.Arrangement nullAssemblies_Arrangement = null;
            componentPositioner1.PrimaryArrangement = nullAssemblies_Arrangement;
            componentPositioner1.EndAssemblyConstraints();
            //
            theUFSession.Modl.Update();
        }

        public static void setpaz_SetHelpPoint (UFSession theUFSession, Tag face_id, out Point3d helpPoint)
        {
            double[] face_point = new double[3];
            double[] face_dir = new double[3];
            double[] face_u1 = new double[3];
            double[] face_v1 = new double[3];
            double[] face_u2 = new double[3];
            double[] face_v2 = new double[3];
            double[] face_norm_dir = new double[3];
            double[] face_rad_data = new double[2];
            double[] face_type = new double[2];

            //set help point
            theUFSession.Modl.AskFaceProps(face_id, face_type, face_point, face_u1, face_v1, face_u2, face_v2, face_norm_dir, face_rad_data);
            helpPoint = new Point3d(face_point[0], face_point[1], face_point[2]);
           // constraintReference1.HelpPoint = helpPoint;
        }

        public static void Main()
        {

            theUFSession = UFSession.GetUFSession();
            theUI = UI.GetUI();

            Tag target_face, target_body;
            double[] cursor_face = new double[3];
            double[] cursor_body = new double[3];

            try
            {

                //получение плоскости компонента "Plita 7081-2202" черезе меню SimplSelect
                NXOpen.UF.UFUi.SelectionOption opts = new NXOpen.UF.UFUi.SelectionOption();
                setpaz_SetSelectionOption(71, 22, 20, out opts); //face
                setpaz_GetObject("face", theUFSession, opts, out target_face, out cursor_face);

                

                //получение компонента "Prokladka 7052-0052" черезе меню SimplSelect
                NXOpen.UF.UFUi.SelectionOption opts1 = new NXOpen.UF.UFUi.SelectionOption();
                setpaz_SetSelectionOption(70, 0, 0, out opts1); //body
                setpaz_GetObject("body", theUFSession, opts1, out target_body, out cursor_body);

                theUFSession.Disp.SetHighlight(target_face, 0);
                theUFSession.Disp.SetHighlight(target_body, 0);

                //Поиск места крепления
              
                //Получение имен установочных плоскостей
                //plita
                string[] seating_face = new string[6];
                setpaz_AskSeatingFaceName(target_face, out seating_face); //здесь по выделенной плоскости находятся все соприкасающиеся плоскости
                //prokladka
                string[] seating_body = {"TOP_1_0", "TOP_1_1", "SIDE_1_0", "SIDE_1_1", "FRONT_1_0", "FRONT_1_1"}; 

                //Получение компонента на котором находится плоскости
                NXOpen.Assemblies.Component component_plita;
                setpaz_AskFaceComponent(target_face, theUFSession, out component_plita);

                NXOpen.Assemblies.Component component_prokladka;
                setpaz_AskFaceComponent(target_body, theUFSession, out component_prokladka);
               
                //Нахождение 3, 5 или 6 установочных плоскостей на объекте  
                Face[] faces_plita = new Face[6];
                setpaz_CheckSeatingFaceComponent(component_plita, seating_face, out faces_plita);
                Face[] faces_prokladka = new Face[6];
                setpaz_CheckSeatingFaceComponent(component_prokladka, seating_body, out faces_prokladka);
                
                
                //Установка ограничителей (Constraints)
                /*
                 
                 faces_plita[0] + faces_prokladka[0]  = TOP_5_0 + TOP_1_0  >>>> на дистанцию 0               >> Работает!!! <<
                 faces_plita[5] + faces_prokladka[2]  = FRONT_5_1 + SIDE_1_0 >>>> touch                      >> Работает!!! <<
                 faces_plita[3] + faces_prokladka[5]  = SIDE_5_1 + FRONT_1_1 >>>> на дистанцию 10  [этап 1]  >> Работает!!! <<
                 
                 faces_plita[3] + faces_prokladka[6]  = SIDE_1_1 + FRONT_1_1 >>>> на дистанцию cursor = 63  [этап 2] 
                  - можно попробовать не закреплять, а поставить в указанное место, позволить ей скользить в доль touch
                 
                 [этап 3]
                 Предложить разные варианты сопряжения, с учетом того, что не все плоскости будут найдены
                  
                 [этап 4]
                 к какой плоскоти ближе курсор туда и устанавливать деталь
                 
                 [ ? ] 
                 Как воспользоваться поворотом - инверсией ограничителя через менюшку ?
                 Снять выделение с плоскости и объекта ?
                 Как это все вяжется с направлением детали ???
                  
                */



                setpaz_CreateConstraint(theUFSession, component_plita, faces_plita[0], component_prokladka, faces_prokladka[0], "Distance", 0);
                setpaz_CreateConstraint(theUFSession, component_plita, faces_plita[5], component_prokladka, faces_prokladka[2], "Touch Align", 0);
                setpaz_CreateConstraint(theUFSession, component_plita, faces_plita[3], component_prokladka, faces_prokladka[5], "Distance", 10);
               


                //theUFSession.Modl.Update();

            }
            catch (NXOpen.NXException ex)
            {
                theUI.NXMessageBox.Show("PAZ", NXMessageBox.DialogType.Error, ex.Message);
            }
        }
    }
}
