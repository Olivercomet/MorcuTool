using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MorcuTool
{
    public class XML
    {
        public enum ReadMode { 
        WAITING_FOR_TAG,
        READING_TAG
        }

        public List<XMLtag> tags = new List<XMLtag>();

        public XMLtag GetFirstRootTagWithName(string name) {
            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i].name.ToLower() == name.ToLower())
                {
                    return tags[i];
                }
            }
            return null;
        }

        public List<XMLtag> GetAllRootTagsWithName(string name)
        {
            List<XMLtag> output = new List<XMLtag>();
            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i].name.ToLower() == name.ToLower())
                {
                    output.Add(tags[i]);
                }
            }
            return output;
        }

        public XML(string input) {

            input = input.Trim();
            int pos = 0;
            ReadMode readMode = ReadMode.WAITING_FOR_TAG;

            XMLtag currentTag = null;

            int startOfIntraTagData = 0;

            while (pos < input.Length) {

                switch (readMode) {

                    case ReadMode.WAITING_FOR_TAG:

                        if (input[pos] != '<')
                        {
                            pos++;
                        }
                        else {
                            readMode = ReadMode.READING_TAG;

                            if (currentTag != null && pos - startOfIntraTagData > 0) {
                                currentTag.data = input.Substring(startOfIntraTagData, pos - startOfIntraTagData).Trim();
                                if (currentTag.data == ""){
                                    currentTag.data = null;
                                }  
                            }
                            continue;
                        }
                        break;

                    case ReadMode.READING_TAG:
                        
                        pos++; //skip opening '<'
                        int TagDataStartPos = pos;
                        int TagDataLength = 0;
                        bool withinQuotationMarks = false;

                        while (!(input[pos] == '>' && !withinQuotationMarks)) { //until we reach the end of the tag and have left quotation marks
                            if (input[pos] == '\"') {
                                withinQuotationMarks = !withinQuotationMarks;
                            }
                            pos++;
                            TagDataLength++;
                        }
                        pos++;

                        //test what kind of tag this is

                        if (input[TagDataStartPos] == '?')
                        {
                            currentTag = new XMLtag(input.Substring(TagDataStartPos+1, TagDataLength-2), currentTag);
                            
                            if (currentTag.parent == null){  //then we are at the root level
                                tags.Add(currentTag);
                            }
                            else {
                                currentTag.parent.children.Add(currentTag);
                            }
                            currentTag = currentTag.parent; //just go back to the parent because a tag with '?' as the first char never has any children
                        }
                        else if (input[TagDataStartPos] == '/') //end the current tag and go back up
                        {
                            currentTag = currentTag.parent;
                        }
                        else {
                            bool goUp = false;

                            if (input[TagDataStartPos + (TagDataLength - 1)] == '/'){ //a tag with no children
                                currentTag = new XMLtag(input.Substring(TagDataStartPos, TagDataLength-1), currentTag);
                                goUp = true;
                            }
                            else {
                                currentTag = new XMLtag(input.Substring(TagDataStartPos, TagDataLength), currentTag);
                            }

                            if (currentTag.parent == null) {  //then we are at the root level
                                tags.Add(currentTag);
                            }
                            else {
                                currentTag.parent.children.Add(currentTag);
                            }

                            if (goUp) { //this was a one-and-done tag and ended itself, so return to the parent
                                currentTag = currentTag.parent;
                            }
                        }

                        readMode = ReadMode.WAITING_FOR_TAG;
                        startOfIntraTagData = pos;
                        break;
                }
            }
            Console.WriteLine("Finished parsing XML");
        }


        public class XMLtag {

            public string name;
            public ParamKeyValuePair[] myParams;
            public string data;

            public XMLtag parent;
            public List<XMLtag> children = new List<XMLtag>();

            public XMLtag(string _params, XMLtag _parent)
            {
                string[] rawParams = _params.Split(' ');
                name = rawParams[0];

                parent = _parent;

               /* if (parent != null) {
                    Console.WriteLine("new tag: " + name + ", child of " + parent.name);
                }
                else {
                    Console.WriteLine("new tag: " + name + " at the root level");
                }*/

                myParams = new ParamKeyValuePair[rawParams.Length - 1];

                for (int i = 1; i < rawParams.Length; i++) {
                    myParams[i - 1] = new ParamKeyValuePair(rawParams[i]);
                    //Console.WriteLine(myParams[i - 1]);
                }
            }

            public class ParamKeyValuePair {

                public string key;
                public string value;

                public ParamKeyValuePair(string input) {
                    string[] splitInput = input.Split('=');
                    key = splitInput[0].Trim();
                    value = splitInput[1].Trim();
                }
            }

            public string GetParamValue(string paramKey) {

                foreach (ParamKeyValuePair param in myParams) {
                    if (param.key.ToLower() == paramKey.ToLower()) {
                        if (param.value[0] == '\"'){
                            return param.value.Substring(1, param.value.Length - 2);
                        }
                        else {
                            return param.value;
                        }
                    }
                }
                return null;
            }

            public XMLtag GetFirstChildWithParamAndValue(string key, string value) {
                for (int i = 0; i < children.Count; i++){
                    foreach (ParamKeyValuePair param in children[i].myParams){
                        if (param.key.ToLower() == key.ToLower() && param.value.ToLower() == value.ToLower()){
                            return children[i];
                        }
                    }
                }
                return null;
            }

            public XMLtag GetFirstChildWithName(string name) {
                for (int i = 0; i < children.Count; i++) {
                    if (children[i].name.ToLower() == name.ToLower()) {
                        return children[i];
                    }
                }
                return null;
            }

            public List<XMLtag> GetChildrenWithName(string name)
            {
                List<XMLtag> output = new List<XMLtag>();
                for (int i = 0; i < children.Count; i++) {
                    if (children[i].name.ToLower() == name.ToLower()) {
                        output.Add(children[i]);
                    }
                }
                return output;
            }
        }
    }
}
