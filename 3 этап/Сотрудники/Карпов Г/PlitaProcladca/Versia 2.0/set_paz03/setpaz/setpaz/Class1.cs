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
        // class members
        private static Session theSession;
        public static Class1 theProgram;
        public static bool isDisposeCalled;

        private static UI theUI;
        private static UFSession theUFSession;
        public static Tag targets;


        //------------------------------------------------------------------------------
        // Constructor
        //------------------------------------------------------------------------------
        public Class1()
        {
            try
            {
                theSession = Session.GetSession();
                isDisposeCalled = false;
            }
            catch (NXOpen.NXException ex)
            {
                // ---- Enter your exception handling code here -----
                UI.GetUI().NXMessageBox.Show("Message", NXMessageBox.DialogType.Error, ex.Message);
            }
        }


        //------------------------------------------------------------------------------
        // Funny functions
        //------------------------------------------------------------------------------

        private static void setpaz_debug(string text, string index)
        {

            using (System.IO.StreamWriter file1 = new System.IO.StreamWriter(@"C:\!_Programming\test\debug" + index + ".txt"))
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
                if (response == 2)
                {
                    break;
                }
            }
            while (response != 5 && i != 0);

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

        public static void setpaz_SetHelpPoint(UFSession theUFSession, Tag face_id, out Point3d helpPoint)
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

        public static void setpaz_HideHelpLine(int constraint_id)
        {
            Session theSession = Session.GetSession();
            Part workPart = theSession.Parts.Work;
            DisplayableObject[] objects3 = new DisplayableObject[1];
            NXOpen.Positioning.DisplayedConstraint displayedConstraint3 = (NXOpen.Positioning.DisplayedConstraint)workPart.FindObject("ENTITY 160 " + constraint_id + " 1");
            objects3[0] = displayedConstraint3;
            theSession.DisplayManager.BlankObjects(objects3);
        }

        public static void setpaz_SetDetailByCursor(UFSession theUFSession, Face face, double[] cursor_face, out int distance_exp)
        {
            int norm_dir, type;
            double[] point = new double[3];
            double[] dir = new double[3];
            double[] box = new double[6];
            double radius, rad_data;
            distance_exp = 0;

            theUFSession.Modl.AskFaceData(face.Tag, out type, point, dir, box, out radius, out rad_data, out norm_dir);

            string[] str_name = face.Name.Split(new Char[] { '_' });

            if (str_name[0] == "SIDE")
            {
                distance_exp = Convert.ToInt32(cursor_face[0] - point[0]);
            }
            else if (str_name[0] == "FRONT")
            {
                distance_exp = Convert.ToInt32(cursor_face[1] - point[1]);
            }
            /*
            if (distance_exp < 0) {
                distance_exp *= -1;  // если деталь слева от плиты, то ставится нормально, иначе уходит в сторону
            }
             */ 
        }

        public static void setpaz_FindAlignTouch(UFSession theUFSession, Face[] faces_plita, Face[] faces_prokladka, double[] cursor_face, out Face[] plita_faces_settings, out Face[] prokladka_faces_settings)
        {
            int norm_dir, type;
            double[] point = new double[3];
            double[,] faces_point = new double[1, 2];
            double[,] face_near_cursor = new double[1, 2];
            double[] dir = new double[3];
            double[] box = new double[6];
            double radius, rad_data;
            plita_faces_settings = new Face[3];
            prokladka_faces_settings = new Face[3];
            Face target_face_align = null, target_dist_face=null;
            int ij = 0;
            string[] name_face = new string[3];
            string[] name_tfa = new string[3];
            string[] name_pf = new string[3];

            int kk=0;
            for (int k = 0; k <= faces_plita.Length - 1; k++ )
            {
                if (faces_plita[k] != null) {
                    kk++;    
                }
            }

            plita_faces_settings[0] = (faces_plita[0] != null) ? faces_plita[0] : null;
            prokladka_faces_settings[0] = (faces_prokladka[0] != null) ? faces_prokladka[0] : null;

            //Поиск Face для Align Touch на plita относительно курсора
            for (int i = 0; i <= faces_plita.Length - 1; i++)
            {
                try
                {
                    if (faces_plita[i] != null)
                    {
                        name_face = faces_plita[i].Name.Split(new Char[] { '_' });

                        if (name_face[0] != "TOP")
                        {
                            theUFSession.Modl.AskFaceData(faces_plita[i].Tag, out type, point, dir, box, out radius, out rad_data, out norm_dir);

                            faces_point[0, 0] = cursor_face[0] - point[0];
                            faces_point[0, 1] = cursor_face[1] - point[1];

                            if (faces_point[0, 0] < 0)
                            {
                                faces_point[0, 0] *= -1;
                            }
                            if (faces_point[0, 1] < 0)
                            {
                                faces_point[0, 1] *= -1;
                            }

                            if (ij == 0)
                            {
                                face_near_cursor[0, 0] = faces_point[0, 0];
                                face_near_cursor[0, 1] = faces_point[0, 1];
                                target_face_align = faces_plita[i];
                            }
                            else
                            {
                                if (face_near_cursor[0, 0] >= faces_point[0, 0] && face_near_cursor[0, 1] >= faces_point[0, 1])
                                {
                                    target_face_align = faces_plita[i];
                                }
                            }
                            ij++;
                        }
                    }
                }
                catch (NXOpen.NXException ex)
                {
                    //
                }
            }
            plita_faces_settings[1] = target_face_align;

            //Поиск Face для Distance на plita
            name_tfa = (target_face_align != null) ? target_face_align.Name.Split(new Char[] { '_' }) : null;
            for (int i_i = 0; i_i <= faces_plita.Length - 1; i_i++)
            {
                try
                {
                    if (faces_plita[i_i] != null)
                    {
                        name_pf = faces_plita[i_i].Name.Split(new Char[] { '_' });
                        if ((name_tfa[0] == "SIDE" && name_pf[0] == "FRONT") || (name_tfa[0] == "FRONT" && name_pf[0] == "SIDE"))
                        {
                            target_dist_face = faces_plita[i_i];
                            break;
                        }
                    }
                }
                catch (NXOpen.NXException ex)
                {
                    //setpaz_debug("5", "0");
                    //theUI.NXMessageBox.Show("PAZ", NXMessageBox.DialogType.Error, ex.Message);

                }
            }
            plita_faces_settings[2] = target_dist_face;
            /*
            //Поиск Face для Align Touch и Distance на prokladka
            name_tfa = plita_faces_settings[1].Name.Split(new Char[] {'_'});
            name_pf = plita_faces_settings[2].Name.Split(new Char[] { '_' });
            string index_face = ( name_tfa[2] == "0") ? "1" : "0" ;
            string index_face1 = (name_pf[2] == "0") ? "1" : "0";

            if (name_tfa[0] == "SIDE") {
                for (int ijik = 0; ijik<=faces_prokladka.Length-1; ijik++) {
                    name_face = faces_prokladka[ijik].Name.Split(new Char[] {'_'});
                    if (name_face[0] == "FRONT" && name_face[2] == index_face)
                    {
                        prokladka_faces_settings[1] = faces_prokladka[ijik];
                    }
                    else if (name_face[0] == "SIDE" && name_face[2] == index_face1)
                    {
                        prokladka_faces_settings[2] = faces_prokladka[ijik];
                    }
                }
            }
            else if (name_tfa[0] == "FRONT")
            {
                for (int iji = 0; iji <= faces_prokladka.Length - 1; iji++)
                {
                    name_face = faces_prokladka[iji].Name.Split(new Char[] { '_' });
                    if (name_face[0] == "SIDE" && name_face[2] == index_face)
                    {
                        prokladka_faces_settings[1] = faces_prokladka[iji];
                    }
                    else if (name_face[0] == "FRONT" && name_face[2] == index_face1)
                    {
                        prokladka_faces_settings[2] = faces_prokladka[iji];
                    }
                }
            }
            */
            /*Замечано, что деталь встает в угол правильно, если сначало цепляется FRONT 
              Но тогда во всех остальных случаях исключается установка детали наоборот...
             */
            /*
            if (name_tfa[0] == "SIDE" && kk <= 3)
            {
                Face[] face_change = new Face[2];
                //переворот значений для плиты
                face_change[0] = plita_faces_settings[1];
                plita_faces_settings[1] = plita_faces_settings[2];
                plita_faces_settings[2] = face_change[0];
                //переворот значений для прокладки
                face_change[1] = prokladka_faces_settings[1];
                prokladka_faces_settings[1] = prokladka_faces_settings[2];
                prokladka_faces_settings[2] = face_change[1];
            }
             */
        }

        //------------------------------------------------------------------------------
        //  Explicit Activation
        //      This entry point is used to activate the application explicitly
        //------------------------------------------------------------------------------
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
                string[] seating_body = { "TOP_1_0", "TOP_1_1", "SIDE_1_0", "SIDE_1_1", "FRONT_1_0", "FRONT_1_1" };

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

                /*
                 
                 faces_plita[0] + faces_prokladka[0]  = TOP_5_0 + TOP_1_0  >>>> на дистанцию 0               >> Работает!!! <<
                 faces_plita[5] + faces_prokladka[2]  = FRONT_5_1 + SIDE_1_0 >>>> touch                      >> Работает!!! <<
                 faces_plita[3] + faces_prokladka[5]  = SIDE_5_1 + FRONT_1_1 >>>> на дистанцию 10  [этап 1]  >> Работает!!! <<
                 
                 faces_plita[3] + faces_prokladka[6]  = SIDE_1_1 + FRONT_1_1 >>>> на дистанцию cursor = 63  [этап 2]  >> Работает!!! <<
                  - можно попробовать не закреплять, а поставить в указанное место, позволить ей скользить в доль touch 
                 
                 [этап 3]
                 Предложить разные варианты сопряжения, с учетом того, что не все плоскости будут найдены               >> Работает!!! <<
                  
                 [этап 4]
                 к какой плоскоти ближе курсор туда и устанавливать деталь
                 
                 [или]
                 - после создания констрейнтов, 
                    вывести менюшку с другими вариантами креплений к данной TOP плоскости 
                    и инверсии вставляемой детали.
                 - Менюшка должна реагировать на OK, Apply, Cancel
                    Cancel - полная отмена констрейнтов для вставленной детали
                 - Попробовать привязать красивую менюшку из Block UI styler
                 - 4 вариантов креплений у средней плиты, + инверсия прокладки *2 = 8 вариантов
                 - проверка на выход прокладки из периметра плиты !!!, если вышла, значит инвертировать прокладку по FRONT
                 
                 [!!!]
                 Disponce dll-ки!!!
                 проверить крепления к боковым плоскостям  >> Работает!!! <<
                 Если выбрана даже не именованная плоскость, просканировать плоскости рядом и найти TOP, а дальше уже думать!
                 !!! Надо найти Углы !!! и для них разработать способ крепления, а во всех остальных случаях без ограничений!
                 отремонтировать размещение относительно курсора
                  
                 [ ? ] 
                 Как воспользоваться поворотом - инверсией ограничителя через менюшку ?
                 Как это все вяжется с направлением детали ???
                Покрасить деталь!!!
                 
                  
                  
                 --------------------------------------------------------------------                
                    Имена                         Индексы
                                        Plita               Prokladka
                  TOP_1_0                 b0                    d0
                  TOP_1_1                                       d1
                  SIDE_1_0                b2                    d2 
                  SIDE_1_1                b3                    d3
                  FRONT_1_0               b4                    d4
                  FRONT_1_1               b5                    d5
                  
                  1. положение прокладки:       d0 d5 d2
                  2. инверсия:                  d1 d4 d3
                  
                  1. плита левый верхний угол:  b0 b3 b5  
                  1. правый верхний угол:       b0 b2 b5  
                  1. левый нижний угол:         b0 b4 b3  
                  1. правый нижний угол:        b0 b2 b4  
                 
                  
                 */

                //подбор вариантов крепления
                //top



                /* //side and front
                if (faces_plita[2] != null) {
                    comp1_face_constraint[1] = faces_plita[2];
                    comp1_face_constraint[2] = (faces_plita[4] == null) ? faces_plita[5] : faces_plita[4];
                }
                else if (faces_plita[3] != null)
                {
                    comp1_face_constraint[1] = faces_plita[3];
                    comp1_face_constraint[2] = faces_plita[5];
                }
                else if (faces_plita[4] != null)
                {
                    comp1_face_constraint[1] = faces_plita[4];
                    comp1_face_constraint[2] = faces_plita[3];
                }   
                */

                Face[] plita_faces_settings = new Face[3];
                Face[] prokladka_faces_settings = new Face[3];

                int distance_exp = 0;


                //Получение 2ой и 3ей плоскости для крепления детали
                setpaz_FindAlignTouch(theUFSession, faces_plita, faces_prokladka, cursor_face, out plita_faces_settings, out prokladka_faces_settings);
                setpaz_SetDetailByCursor(theUFSession, plita_faces_settings[2], cursor_face, out distance_exp);

                //установка ограничителей
                //052, 143
                /*
                theUI.NXMessageBox.Show("PAZ", NXMessageBox.DialogType.Error,   
                        prokladka_faces_settings[0].Name + " + " + plita_faces_settings[0].Name + " ;\n "
                          + prokladka_faces_settings[2].Name + " + " + plita_faces_settings[2].Name + " ;\n "
                          + prokladka_faces_settings[1].Name + " + " + plita_faces_settings[1].Name + " ;\n "
                    );
                 * */
                //!!!! Заставить деталь поворячиваться!!!
                // Не правильно ставится деталь по курсору!!!, даже когда указываешь ребро паза, всеравно в другом пазе устанавливается!!!!!

                // Выше центра плиты должно быть 052, а ниже 143
                setpaz_CreateConstraint(theUFSession, component_prokladka, faces_prokladka[1], component_plita, plita_faces_settings[0], "Distance", 0);
                setpaz_CreateConstraint(theUFSession, component_prokladka, faces_prokladka[4], component_plita, plita_faces_settings[2], "Distance", distance_exp);
                setpaz_CreateConstraint(theUFSession, component_prokladka, faces_prokladka[3], component_plita, plita_faces_settings[1], "Touch Align", 0);
                

                //скрыть десять некрасивых вспомогательных линий констрейнта
                /*   int[] id = { 3, 6, 12, 15, 18, 21, 24, 27, 30 };
                   for(int i=0; i<=id.Length-1; i++)
                   {
                       try { 
                           setpaz_HideHelpLine(id[i]);
                       }
                       catch (NXOpen.NXException ex) {
                           //
                       }
                   }
                   */
                theUFSession.Modl.Update();

                
               
            }
            catch (NXOpen.NXException ex)
            {
                theUI.NXMessageBox.Show("PAZ", NXMessageBox.DialogType.Error, ex.Message);
            }

            theProgram = new Class1();
            theProgram.Dispose();
        }

        //------------------------------------------------------------------------------
        // Following method disposes all the class members
        //------------------------------------------------------------------------------

        public void Dispose()
        {
            theUI = UI.GetUI();
            try
            {
                if (isDisposeCalled == false)
                {
                    //TODO: Add your application code here 
                }
                isDisposeCalled = true;
            }
            catch (NXOpen.NXException ex)
            {
                theUI.NXMessageBox.Show("PAZ", NXMessageBox.DialogType.Error, ex.Message);
            }
        }



        public static int GetUnloadOption(String dummy)
        {
            return UFConstants.UF_UNLOAD_UG_TERMINATE;
        }


    }
}