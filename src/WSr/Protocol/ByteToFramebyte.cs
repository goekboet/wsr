namespace WSr.Protocol
{
    public static class FrameByteFunctions
    {
        public static Either<FrameByte> FrameFirst(byte b) => new Either<FrameByte>(FrameByte.Empty);

    }
}