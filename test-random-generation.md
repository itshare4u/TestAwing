# Test Random Data Generation

## 🎯 Updated Logic

The random data generation now supports flexible parameters where:
- **n**: Number of rows 
- **m**: Number of columns
- **p**: Maximum chest number

## ✅ New Constraints

1. **n×m ≥ p**: Matrix must have enough positions for all chest numbers
2. **Each number 1 to p appears at least once**: Ensures solvability
3. **Remaining positions filled randomly**: Can have duplicates from 1 to p

## 🧪 Test Cases

### Case 1: Small Matrix with Few Chests
- **Parameters**: n=3, m=3, p=5
- **Matrix size**: 9 positions
- **Result**: Numbers 1-5 each appear at least once, remaining 4 positions filled randomly

### Case 2: Large Matrix with Many Chests  
- **Parameters**: n=4, m=4, p=12
- **Matrix size**: 16 positions
- **Result**: Numbers 1-12 each appear at least once, remaining 4 positions filled randomly

### Case 3: Exact Match
- **Parameters**: n=3, m=3, p=9
- **Matrix size**: 9 positions
- **Result**: Numbers 1-9 each appear exactly once

### Case 4: Invalid (Will Fail)
- **Parameters**: n=2, m=2, p=5
- **Matrix size**: 4 positions < 5 chests
- **Result**: Error - not enough positions

## 🔄 API Testing

```bash
# Test with custom parameters
curl "http://localhost:5001/api/generate-random-data?n=4&m=4&p=6"

# Test with defaults
curl "http://localhost:5001/api/generate-random-data"

# Test invalid case
curl "http://localhost:5001/api/generate-random-data?n=2&m=2&p=5"
```

## 🎨 Frontend Changes

- Random Data button now uses current n, m, p values from form
- No longer forces p = n×m
- Shows validation errors if constraints not met
- Path visualization works with any valid configuration
