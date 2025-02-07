fn main() {
    _ = csbindgen::Builder::default()
        .input_extern_file("src/lib.rs")
        .csharp_dll_name("joycon_quat")
        .csharp_namespace("JoyconQuat")
        .csharp_class_name("NativeMethod")
        .generate_csharp_file("../joycon-quat-bin/JoyconQuat.g.cs");
}
