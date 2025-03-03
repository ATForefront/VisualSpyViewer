use joycon_quat::Quaternion;

/// # Safety
/// buffer_ptr は 18byte の 加速度を除いた情報を詰める必要がある。
#[no_mangle]
pub unsafe extern "C" fn quaternion_parse(buffer_ptr: *const u8) -> QuaternionParseResultFFI {
    let Ok(buffer_slice) = std::slice::from_raw_parts(buffer_ptr, 18).try_into() else {
        return QuaternionParseResultFFI {
            is_ok: false,
            ..Default::default()
        };
    };
    let Some(unpacked) = Quaternion::parse(buffer_slice) else {
        return QuaternionParseResultFFI {
            is_ok: false,
            ..Default::default()
        };
    };

    let time_stamp_start = unpacked.1.start().value();
    let time_stamp_count = unpacked.1.count().value();
    let quat_0 = unpacked.0[0].clone().into();
    let quat_1 = unpacked.0[1].clone().into();
    let quat_2 = unpacked.0[2].clone().into();

    return QuaternionParseResultFFI {
        is_ok: true,
        time_stamp: TimestampFFI {
            start: time_stamp_start,
            count: time_stamp_count,
        },
        zero: quat_0,
        one: quat_1,
        two: quat_2,
    };
}

#[repr(C)]
#[derive(Debug, Default, Clone)]
pub struct QuaternionParseResultFFI {
    is_ok: bool,
    zero: QuaternionFFI,
    one: QuaternionFFI,
    two: QuaternionFFI,
    time_stamp: TimestampFFI,
}

#[repr(C)]
#[derive(Debug, Default, Clone)]
pub struct QuaternionFFI {
    x: f32,
    y: f32,
    z: f32,
    w: f32,
}
impl Into<QuaternionFFI> for Quaternion {
    fn into(self) -> QuaternionFFI {
        QuaternionFFI {
            x: self.0[0].to_num::<f32>(),
            y: self.0[1].to_num::<f32>(),
            z: self.0[2].to_num::<f32>(),
            w: self.0[3].to_num::<f32>(),
        }
    }
}

#[repr(C)]
#[derive(Debug, Default, Clone, PartialEq, Copy)]
pub struct TimestampFFI {
    start: u16,
    count: u8,
}
