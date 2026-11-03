namespace II.RPA.Plugin.JavaAlter.Agent
{
    internal class FakeJavaObjectWrapper
    {
        public FakeJavaObjectWrapper(string value)
        {
            this.toStringValue = value;
        }

        public override bool Equals(object obj)
        {
            FakeJavaObjectWrapper fakeJavaObjectWrapper = obj as FakeJavaObjectWrapper;
            if (fakeJavaObjectWrapper != null)
            {
                return fakeJavaObjectWrapper.toStringValue == this.toStringValue;
            }
            JavaObjectWrapper javaObjectWrapper = obj as JavaObjectWrapper;
            if (javaObjectWrapper != null)
            {
                try
                {
                    return javaObjectWrapper.ToString() == this.toStringValue;
                }
                catch
                {
                    return false;
                }
                return false;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return this.toStringValue.GetHashCode();
        }

        public override string ToString()
        {
            return this.toStringValue;
        }

        private readonly string toStringValue;
    }
}
