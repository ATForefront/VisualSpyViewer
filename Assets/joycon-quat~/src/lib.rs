mod bilge_util;
mod types;

use std::ops::{Add, Sub};

use az::CastFrom;
use bilge::prelude::*;
use bilge_util::BilgeBitvecExt;
use bitvec::{array::BitArray, order::Lsb0};
use fixed::traits::{Fixed, FixedBits, FixedSigned};
use fixed_sqrt::FixedSqrt;
use nalgebra::{Unit, UnitQuaternion};
use rand::{thread_rng, Rng};
use types::*;

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

fn main() {
    let mut rng = thread_rng();
    let quat: UnitQuaternion<f32> = Unit::new_normalize(rng.gen());
    //let quat = Quaternion(res.coords.data.0[0].map(FP30::cast_from));

    let diff = UnitQuaternion::<f32>::from_axis_angle(&Unit::new_normalize(rng.gen()), 0.001);

    let quats = [diff * quat, quat, quat * diff]
        .map(|q| Quaternion(q.coords.data.0[0].map(FP30::cast_from)));

    let timestamp = Timestamp::new(
        u11::new(rng.gen::<u16>() & u11::MASK),
        u6::new(rng.gen::<u8>() & u6::MASK),
    );
    let buf = compress_quaternion_triplet(&quats, timestamp);

    let (parsed_quats, parsed_timestamp) = Quaternion::parse(buf).unwrap();
    assert_eq!(timestamp, parsed_timestamp);

    println!("before: {:?}\nafter: {:?}", quats, parsed_quats);
}

type FP30 = fixed::types::I2F30;

// note: should be some kinda signed fixed point type
#[derive(Debug, Default, Clone)]
pub struct Quaternion([FP30; 4]);

impl Quaternion {
    // QuaternionFp30MaxAbsQfp
    fn max_abs_idx(&self) -> usize {
        let mut index = 0;
        let mut max = self.0[0].abs();
        for i in 1..=3 {
            let val = self.0[i].abs();
            if val > max {
                index = i;
                max = val;
            }
        }
        return index;
    }

    // QuaternionFp30ConjugateQfp
    fn to_quat3(&self, max_idx: usize, out: &mut Quat3) {
        let sign = self.0[max_idx].signum();
        for i in 1..=3 {
            // negating a quaternion produces an identical rotation
            out.0[i - 1] = self.0[(i + max_idx) & 3] * sign;
        }
    }

    fn reconstruct(item: Quat3, missing_idx: usize) -> Quaternion {
        let mut sqr_sum = FP30::ONE;
        let mut out = Quaternion::default();
        for i in 1..=3 {
            out.0[(i + missing_idx) & 3] = item.0[i - 1];
            sqr_sum -= item.0[i - 1] * item.0[i - 1];
        }
        //    a**2 + b**2 + c**2 + d**2 = 1
        // => a**2 + b**2 + c**2 - 1 = -d**2
        // => sqrt(1 - a**2 - b**2 - c**2) = d
        // TODO: compare cordic and fixed_sqrt at some point? or just do this in flaoting point
        out.0[missing_idx] = sqr_sum.sqrt();

        out
    }

    pub fn parse(bytes: [u8; 18]) -> Option<([Quaternion; 3], types::Timestamp)> {
        let bits = BitArray::<_, Lsb0>::new(bytes);
        let bits = bits.as_bitslice();
        let (mode, mut bits) = QuaternionMode::read(bits);
        let quats = match dbg!(mode) {
            QuaternionMode::Individual => {
                let (data, rest) = Individual::read(bits);
                bits = rest;
                data.val_0().map(|q| {
                    let (idx, components) = q.unpack();
                    let idx = idx.value() as usize;
                    Self::reconstruct(components.into(), idx)
                })
            }
            QuaternionMode::FirstLastDeltaMid => {
                let (data, rest) = FirstLastDeltaMid::read(bits);
                bits = rest;
                let (idx, data) = data.unpack();
                let idx = idx.value() as usize;
                data.map(|q| Self::reconstruct(q.into(), idx))
            }
            QuaternionMode::LastDeltaFirstDeltaMid => {
                let (data, rest) = LastDeltaFirstDeltaMid::read(bits);
                bits = rest;
                let (idx, data) = data.unpack();
                let idx = idx.value() as usize;
                data.map(|q| Self::reconstruct(q.into(), idx))
            }
            QuaternionMode::Unknown => {
                return None;
            }
        };

        let (timestamp, _) = Timestamp::read(bits);

        Some((quats, timestamp))
    }
}

// implicit information: index of the missing element
#[derive(Copy, Clone, Default, Debug)]
struct Quat3([FP30; 3]);

// consider merging into Quat3
#[derive(Default, Copy, Clone, Debug, PartialEq)]
pub struct FP3<N: FixedSigned> {
    value: [N; 3],
}

impl<N: FixedSigned> From<Quat3> for FP3<N> {
    // QuaternionFp30ConstructFP3FromQuaternion
    fn from(val: Quat3) -> Self {
        // the largest value that is too small to be represented in the new type
        let epsilon = FP30::ONE >> (N::FRAC_NBITS + 1);
        let value = val.0.map(|c| {
            // rounding away from 0: increment the first bit that's going to be truncated away
            let val = c + c.signum() * epsilon;
            // truncate
            val.to_num()
        });

        Self { value }
    }
}

impl<N: FixedSigned> From<FP3<N>> for Quat3 {
    fn from(val: FP3<N>) -> Quat3 {
        Quat3(val.value.map(N::to_num))
    }
}

impl<N: FixedSigned> Add for FP3<N> {
    type Output = FP3<N>;

    fn add(self, rhs: Self) -> Self::Output {
        FP3 {
            value: std::array::from_fn(|i| self.value[i] + rhs.value[i]),
        }
    }
}

impl<N: FixedSigned> Sub for FP3<N> {
    type Output = FP3<N>;

    fn sub(self, rhs: Self) -> Self::Output {
        FP3 {
            value: std::array::from_fn(|i| self.value[i] - rhs.value[i]),
        }
    }
}

impl<N: FixedSigned> FP3<N> {
    fn avg(&self, other: &FP3<N>) -> FP3<N> {
        let mut res = FP3::default();

        for i in 0..3 {
            let val = self.value[i] + other.value[i];
            res.value[i] = (val + (val.signum() * N::DELTA)) >> 1;
        }

        res
    }

    // QuaternionFp30HasFP3Bit
    fn fits_within(&self, bits: usize) -> bool {
        // returns true if the number can be represented by that number of bits
        let fits =
            |val| (val << N::Bits::BITS - bits as u32) >> (N::Bits::BITS - bits as u32) == val;

        fits(self.value[0]) && fits(self.value[1]) && fits(self.value[2])
    }

    fn minmax(&self) -> (N, N) {
        let mut min = N::ZERO;
        let mut max = N::ZERO;
        for elem in self.value {
            if elem > max {
                max = elem;
            } else if elem < min {
                min = elem;
            }
        }
        (min, max)
    }
}

// QuaternionFp30EncodeUnitQuatTriplet
pub fn compress_quaternion_triplet(input: &[Quaternion; 3], timestamp: Timestamp) -> [u8; 18] {
    let mut res = BitArray::<_, Lsb0>::new([0u8; 18]);
    let mut stream = res.as_mut_bitslice();

    let max_idx = input[1].max_abs_idx();

    let mut working_quat3 = [Quat3([FP30::ZERO; 3]); 3];
    let mut truncated = [FP3 {
        value: [<FP20 as Fixed>::ZERO; 3],
    }; 3];
    for i in 0..3 {
        input[i].to_quat3(max_idx, &mut working_quat3[i]);
        truncated[i] = working_quat3[i].into();
    }

    let delta_first = truncated[2] - truncated[0]; // delta_first = last - first
    let avg = truncated[2].avg(&truncated[0]);
    let diff_1 = truncated[1] - avg; // delta_mid = mid - avg

    // ensure that the method of reconstruction is a parfect match against the truncation
    debug_assert_eq!(truncated[0], truncated[2] - delta_first);
    debug_assert_eq!(truncated[1], avg + diff_1);

    let mut written = false;
    let missing_idx = max_idx as u8;

    // replaces (1 << 29), a number with the first fractional bit set, aka 1/2 in a 30 fractional bit system.
    let half: FP30 = FP30::ONE >> 1;

    if delta_first.fits_within(13) && diff_1.fits_within(7) {
        stream = QuaternionMode::LastDeltaFirstDeltaMid.write_slice(stream);
        stream = LastDeltaFirstDeltaMid::new(
            u2::new(missing_idx),
            truncated[2].into(),
            delta_first.into(),
            diff_1.into(),
        )
        .write_slice(stream);
        written = true;
    } else if input[0].0[max_idx].abs() > half && input[2].0[max_idx] > half {
        let mut truncated = [FP3 {
            value: [<FP15 as Fixed>::ZERO; 3],
        }; 3];
        for i in 0..3 {
            truncated[i] = working_quat3[i].into();
        }
        let avg = truncated[2].avg(&truncated[0]);
        let mut diff_mid = truncated[1] - avg;
        debug_assert_eq!(truncated[1], avg + diff_mid);
        let (min, max) = diff_mid.minmax();
        if max < FP15::from_bits(512) && min >= FP15::from_bits(-512) {
            // remains in-range
            let additional_shift = max >= FP15::from_bits(128) || min < FP15::from_bits(-128);
            if additional_shift {
                for elem in &mut diff_mid.value {
                    *elem >>= 2;
                }
            }

            stream = QuaternionMode::FirstLastDeltaMid.write_slice(stream);
            stream = FirstLastDeltaMid::new(
                additional_shift,
                u2::new(missing_idx),
                truncated[0].into(),
                truncated[2].into(),
                diff_mid.into(),
            )
            .write_slice(stream);
            written = true;
        }
    }

    if !written {
        // QuaternionFp30Unk
        stream = QuaternionMode::Individual.write_slice(stream);
        stream = Individual::new(input.clone().map(|q| {
            let max_idx = q.max_abs_idx();
            let missing_idx = max_idx as u8;

            let mut quat3 = Quat3([<_ as Fixed>::ZERO; 3]);
            q.to_quat3(max_idx, &mut quat3);
            let fp3: FP3<FP12> = quat3.into();
            IndividualCompress::new(u2::new(missing_idx), fp3.into())
        }))
        .write_slice(stream)
    }

    timestamp.write_slice(stream);

    res.data
}
